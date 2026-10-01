using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Planner;

/// <summary>
/// Something to do. The date and the time are optional: a task is either "someday", for a day, or for a moment.
/// The task keeps its schedule consistent by itself: no time without a date, no reminder without a time.
/// A repeated task is never finished: completing it moves it on to its next occurrence.
/// </summary>
public class PlannerTask
{
    public const int MaxTitleLength = 100;

    /// <summary>A day: a reminder earlier than that is no longer about this task.</summary>
    public const int MaxRemindMinutesBefore = 24 * 60;

    private PlannerTask() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public string Title { get; private set; } = null!;

    /// <summary>Null for a task without a topic.</summary>
    public Guid? TopicId { get; private set; }

    /// <summary>In the user's time zone; null for a task without a date.</summary>
    public DateOnly? DueDate { get; private set; }

    /// <summary>In the user's time zone; null for a task for the whole day.</summary>
    public TimeOnly? DueTime { get; private set; }

    /// <summary>Null for a one-time task.</summary>
    public RepeatRule? Repeat { get; private set; }

    /// <summary>Minutes before <see cref="DueTime"/>, 0 at the moment itself; null without a reminder.</summary>
    public int? RemindMinutesBefore { get; private set; }

    /// <summary>Local time "+15 хв" or "+1 год" moved the reminder to; null when not snoozed.</summary>
    public DateTime? SnoozedUntil { get; private set; }

    /// <summary>
    /// Local moment of the last reminder sent. Another date, time or snooze gives another moment,
    /// so the reminder is due again without anybody resetting it.
    /// </summary>
    public DateTime? LastRemindedAt { get; private set; }

    /// <summary>UTC; set when a one-time task is done. Repeated tasks move on instead.</summary>
    public DateTime? CompletedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsDone => CompletedAt is not null;

    /// <param name="dueDate">Null for a task without a date.</param>
    /// <param name="topic">Null for a task without a topic.</param>
    public static PlannerTask Create(long chatId, string title, DateOnly? dueDate, PlannerTopic? topic, DateTime createdAtUtc)
    {
        EnsureSameChat(chatId, topic);

        return new PlannerTask
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Title = Guard.RequiredText(title, MaxTitleLength, nameof(title)),
            TopicId = topic?.Id,
            DueDate = dueDate,
            CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
        };
    }

    public void Rename(string title)
    {
        EnsureOpen();
        Title = Guard.RequiredText(title, MaxTitleLength, nameof(title));
    }

    /// <summary>
    /// Moves the task to another day, keeping its time; a repeated task may be moved off its rule for once.
    /// Null takes the date away, and with it the time, the repeat and the reminder: they all need a date.
    /// </summary>
    public void Schedule(DateOnly? date)
    {
        EnsureOpen();

        DueDate = date;
        if (date is null)
        {
            DueTime = null;
            Repeat = null;
            RemindMinutesBefore = null;
        }

        SnoozedUntil = null;
    }

    /// <summary>Null makes it a task for the whole day, which has no reminder.</summary>
    public void SetTime(TimeOnly? time)
    {
        EnsureOpen();
        if (time is not null && DueDate is null)
            throw new InvalidOperationException("A task needs a date before it gets a time.");

        DueTime = time;
        if (time is null)
            RemindMinutesBefore = null;

        SnoozedUntil = null;
    }

    /// <summary>
    /// The date moves to the first occurrence of the rule: "Mon, Wed, Fri" set on a Tuesday task means Wednesday.
    /// A task without a date starts <paramref name="today"/>. Null makes it a one-time task.
    /// </summary>
    public void SetRepeat(RepeatRule? rule, DateOnly today)
    {
        EnsureOpen();

        Repeat = rule;
        if (rule is not null)
            DueDate = rule.FirstOnOrAfter(DueDate ?? today);

        SnoozedUntil = null;
    }

    /// <param name="minutesBefore">0 reminds at the task's time; null removes the reminder.</param>
    public void SetReminder(int? minutesBefore)
    {
        EnsureOpen();
        if (minutesBefore is not null)
        {
            if (DueTime is null)
                throw new InvalidOperationException("Only a task with a time can have a reminder.");
            if (minutesBefore is < 0 or > MaxRemindMinutesBefore)
                throw new ArgumentOutOfRangeException(nameof(minutesBefore), minutesBefore, $"Must be from 0 to {MaxRemindMinutesBefore}.");
        }

        RemindMinutesBefore = minutesBefore;
        SnoozedUntil = null;
    }

    /// <summary>
    /// Null takes the task out of its topic. Works on done tasks too: a deleted topic must let go of all of them.
    /// </summary>
    public void MoveToTopic(PlannerTopic? topic)
    {
        EnsureSameChat(ChatId, topic);
        TopicId = topic?.Id;
    }

    /// <summary>
    /// A one-time task is done; a repeated one moves on to its next occurrence.
    /// </summary>
    /// <returns>The date that was completed, for <see cref="Reopen"/>; null for a task without a date.</returns>
    public DateOnly? Complete(DateTime completedAtUtc)
    {
        EnsureOpen();

        var completedDate = DueDate;
        if (Repeat is not null && DueDate is { } date)
            DueDate = Repeat.NextAfter(date);
        else
            CompletedAt = Guard.Utc(completedAtUtc, nameof(completedAtUtc));

        SnoozedUntil = null;
        return completedDate;
    }

    /// <summary>
    /// Undo of <see cref="Complete"/>. Only the latest completion of a repeated task can be undone:
    /// after another one the task has already moved further.
    /// </summary>
    /// <param name="completedDate">What <see cref="Complete"/> returned.</param>
    /// <returns>False when there is nothing to undo any more.</returns>
    public bool Reopen(DateOnly? completedDate)
    {
        if (Repeat is null)
        {
            if (!IsDone)
                return false;

            CompletedAt = null;
            return true;
        }

        if (completedDate is not { } date || DueDate != Repeat.NextAfter(date))
            return false;

        DueDate = date;
        SnoozedUntil = null;
        return true;
    }

    /// <summary>
    /// An unfinished task of a past day moves to today and keeps its time, so its reminder comes again today.
    /// </summary>
    /// <returns>False when the task is not overdue.</returns>
    public bool RollOver(DateOnly today)
    {
        if (IsDone || DueDate is not { } date || date >= today)
            return false;

        DueDate = today;
        SnoozedUntil = null;
        return true;
    }

    /// <summary>"+15 хв" and "+1 год" of a reminder: remind once more at <paramref name="localUntil"/>.</summary>
    public void Snooze(DateTime localUntil)
    {
        EnsureOpen();
        SnoozedUntil = localUntil;
    }

    /// <summary>Local moment to remind at; null without a reminder.</summary>
    public DateTime? GetReminderTime()
    {
        if (IsDone)
            return null;
        if (SnoozedUntil is { } snoozed)
            return snoozed;

        return DueDate is { } date && DueTime is { } time && RemindMinutesBefore is { } minutes
            ? date.ToDateTime(time).AddMinutes(-minutes)
            : null;
    }

    /// <param name="localNow">Now in the user's time zone.</param>
    public bool IsReminderDue(DateTime localNow) =>
        GetReminderTime() is { } reminderTime && localNow >= reminderTime && LastRemindedAt != reminderTime;

    public void MarkReminded() => LastRemindedAt = GetReminderTime();

    private void EnsureOpen()
    {
        if (IsDone)
            throw new InvalidOperationException("This task is already done.");
    }

    private static void EnsureSameChat(long chatId, PlannerTopic? topic)
    {
        if (topic is not null && topic.ChatId != chatId)
            throw new ArgumentException("The topic belongs to another chat.", nameof(topic));
    }
}
