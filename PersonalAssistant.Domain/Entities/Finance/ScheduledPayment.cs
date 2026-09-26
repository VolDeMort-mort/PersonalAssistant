using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Finance;

/// <summary>
/// An expense that has to be paid on a date, e.g. rent on the 1st of every month.
/// Paying it records a real transaction and moves the payment to its next date.
/// </summary>
public class ScheduledPayment
{
    public const int MaxTitleLength = 40;
    public const int MaxRemindDaysBefore = 7;

    private ScheduledPayment() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public string Title { get; private set; } = null!;

    /// <summary>Expected amount; the amount actually paid is chosen when paying.</summary>
    public long Amount { get; private set; }

    public Guid CategoryId { get; private set; }
    public Recurrence Recurrence { get; private set; }
    public DateOnly NextDueDate { get; private set; }

    /// <summary>
    /// Day of month the payment belongs to: a payment on the 31st falls on the 28th in February
    /// and returns to the 31st in March.
    /// </summary>
    public int AnchorDay { get; private set; }

    /// <summary>Null when the payment has no reminder.</summary>
    public int? RemindDaysBefore { get; private set; }

    /// <summary>Time in the user's time zone; null when the payment has no reminder.</summary>
    public TimeOnly? RemindAt { get; private set; }

    /// <summary>Due date the last reminder was sent for: one reminder per due date.</summary>
    public DateOnly? LastRemindedFor { get; private set; }

    /// <summary>False once a one-time payment is paid.</summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <param name="remindDaysBefore">Null together with <paramref name="remindAt"/>: no reminder.</param>
    public static ScheduledPayment Create(FinanceCategory category, string title, long amount, Recurrence recurrence,
        DateOnly firstDueDate, int? remindDaysBefore, TimeOnly? remindAt, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (category.Type != TransactionType.Expense)
            throw new ArgumentException("A scheduled payment needs an expense category.", nameof(category));
        if (remindDaysBefore is null != remindAt is null)
            throw new ArgumentException("A reminder needs both the day and the time.", nameof(remindAt));
        if (remindDaysBefore is < 0 or > MaxRemindDaysBefore)
            throw new ArgumentOutOfRangeException(nameof(remindDaysBefore), remindDaysBefore, $"Must be from 0 to {MaxRemindDaysBefore}.");

        return new ScheduledPayment
        {
            Id = Guid.NewGuid(),
            ChatId = category.ChatId,
            Title = Guard.RequiredText(title, MaxTitleLength, nameof(title)),
            Amount = Guard.Positive(amount, nameof(amount)),
            CategoryId = category.Id,
            Recurrence = recurrence,
            NextDueDate = firstDueDate,
            AnchorDay = firstDueDate.Day,
            RemindDaysBefore = remindDaysBefore,
            RemindAt = remindAt,
            IsActive = true,
            CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
        };
    }

    /// <summary>
    /// Records the payment with the amount actually paid and moves on to the next due date;
    /// a one-time payment is finished.
    /// </summary>
    /// <param name="category">The payment's own category.</param>
    public FinanceTransaction Pay(FinanceCategory category, long amount, DateTime paidAtUtc)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (!IsActive)
            throw new InvalidOperationException("This payment is already finished.");
        if (category.Id != CategoryId)
            throw new ArgumentException("The category does not belong to this payment.", nameof(category));

        var transaction = FinanceTransaction.CreateForScheduledPayment(category, amount, Title, Id, NextDueDate, paidAtUtc);

        if (Recurrence == Recurrence.Once)
            IsActive = false;
        else
            NextDueDate = NextDateAfter(NextDueDate);

        return transaction;
    }

    /// <summary>
    /// Undo of <see cref="Pay"/>: the payment is due on <paramref name="paidDueDate"/> again.
    /// Only the latest payment can be reverted: after an older one the schedule has already moved on.
    /// </summary>
    public void RevertPayment(DateOnly paidDueDate)
    {
        var isLatestPayment = Recurrence == Recurrence.Once
            ? !IsActive && NextDueDate == paidDueDate
            : IsActive && NextDueDate == NextDateAfter(paidDueDate);

        if (!isLatestPayment)
            return;

        NextDueDate = paidDueDate;
        IsActive = true;
    }

    /// <summary>Local date and time to remind about the current due date; null without a reminder.</summary>
    public DateTime? GetReminderTime() =>
        IsActive && RemindDaysBefore is { } days && RemindAt is { } time
            ? NextDueDate.AddDays(-days).ToDateTime(time)
            : null;

    /// <param name="localNow">Now in the user's time zone.</param>
    public bool IsReminderDue(DateTime localNow) =>
        GetReminderTime() is { } reminderTime && localNow >= reminderTime && LastRemindedFor != NextDueDate;

    public void MarkReminded() => LastRemindedFor = NextDueDate;

    public bool IsOverdue(DateOnly today) => IsActive && NextDueDate < today;

    private DateOnly NextDateAfter(DateOnly date) => Recurrence switch
    {
        Recurrence.Weekly => date.AddDays(7),
        Recurrence.Monthly => OnAnchorDay(date.AddMonths(1)),
        Recurrence.Yearly => OnAnchorDay(date.AddYears(1)),
        _ => date
    };

    private DateOnly OnAnchorDay(DateOnly date) =>
        new(date.Year, date.Month, Math.Min(AnchorDay, DateTime.DaysInMonth(date.Year, date.Month)));
}
