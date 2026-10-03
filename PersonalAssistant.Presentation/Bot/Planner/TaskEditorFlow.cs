using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Commands;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Features.Planner.Queries;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>
/// The task editor: the new task wizard (title → date → time → repeat → reminder → topic)
/// and editing one field from the card. Both walk the same steps; the wizard goes on to the next one,
/// editing saves that field with its own command and returns to the card.
/// </summary>
public class TaskEditorFlow
{
    private readonly ISender _sender;
    private readonly IDialogStore _dialogs;
    private readonly TimeProvider _time;
    private readonly PlannerFlow _planner;

    public TaskEditorFlow(ISender sender, IDialogStore dialogs, TimeProvider time, PlannerFlow planner)
    {
        _sender = sender;
        _dialogs = dialogs;
        _time = time;
        _planner = planner;
    }

    // ---------- Start ----------

    /// <summary>"➕ Задача". Started in a topic, the task goes to that topic without asking.</summary>
    public async Task StartNewAsync(long chatId, CancellationToken ct)
    {
        var list = _planner.CurrentList(chatId);
        var dialog = new PlannerDialog(list) { Page = _planner.CurrentPage(chatId), Step = EditorStep.Title };

        if (list.TopicId is { } topicId)
        {
            var topics = await _sender.Send(new GetTopicsQuery(chatId), ct);
            if (topics.FirstOrDefault(t => t.Id == topicId) is { } topic)
            {
                dialog.Draft.TopicId = topic.Id;
                dialog.Draft.TopicName = topic.Name;
            }
        }

        await RenderAsync(chatId, dialog, error: null, ct);
    }

    /// <summary>A button of the card: edits one field of the task.</summary>
    public async Task StartEditAsync(long chatId, Guid taskId, TaskField field, CancellationToken ct)
    {
        var task = await _planner.GetTaskAsync(chatId, taskId, ct);

        // The card hides these buttons when they make no sense, but an older card may still show them
        if ((field == TaskField.Time && task.DueDate is null) || (field == TaskField.Reminder && task.DueTime is null))
        {
            await _planner.ShowCardAsync(chatId, taskId, notice: null, ct);
            return;
        }

        var dialog = new PlannerDialog(_planner.CurrentList(chatId))
        {
            Page = _planner.CurrentPage(chatId),
            TaskId = taskId,
            EditedField = field,
            Step = field switch
            {
                TaskField.Title => EditorStep.Title,
                TaskField.Date => EditorStep.Date,
                TaskField.Time => EditorStep.Hour,
                TaskField.Repeat => EditorStep.Repeat,
                TaskField.Reminder => EditorStep.Reminder,
                _ => EditorStep.Topic
            }
        };
        dialog.Draft.Title = task.Title;
        dialog.Draft.Date = task.DueDate;

        await RenderAsync(chatId, dialog, error: null, ct);
    }

    public async Task CancelAsync(long chatId, CancellationToken ct)
    {
        if (_dialogs.Get<PlannerDialog>(chatId) is { EditedField: not null, TaskId: { } taskId })
            await _planner.ShowCardAsync(chatId, taskId, notice: null, ct);
        else
            await _planner.ShowCurrentListAsync(chatId, notice: null, ct);
    }

    // ---------- Date ----------

    public async Task ChooseDateAsync(long chatId, DateOnly date, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Date))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        if (date < _time.GetLocalToday())
        {
            await RenderAsync(chatId, dialog, PlannerView.DateInPastError, ct);
            return;
        }

        await OnDateChosenAsync(chatId, dialog, date, ct);
    }

    public Task OtherDateAsync(long chatId, CancellationToken ct) =>
        MoveAsync(chatId, EditorStep.Month, ct, EditorStep.Date);

    public Task BackToDatesAsync(long chatId, CancellationToken ct) =>
        MoveAsync(chatId, EditorStep.Date, ct, EditorStep.Month, EditorStep.Day);

    /// <param name="yearMonth">E.g. 202610.</param>
    public async Task ChooseMonthAsync(long chatId, int yearMonth, CancellationToken ct)
    {
        var (year, month) = (yearMonth / 100, yearMonth % 100);
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Month) || year < 1 || month is < 1 or > 12)
        {
            await CancelAsync(chatId, ct);
            return;
        }

        dialog.Draft.Month = new DateOnly(year, month, 1);
        dialog.Step = EditorStep.Day;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    public async Task ChooseDayAsync(long chatId, int day, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Day) || dialog.Draft.Month is not { } month
            || day < 1 || day > DateTime.DaysInMonth(month.Year, month.Month))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        var date = new DateOnly(month.Year, month.Month, day);
        if (date < _time.GetLocalToday())
        {
            await RenderAsync(chatId, dialog, PlannerView.DateInPastError, ct);
            return;
        }

        await OnDateChosenAsync(chatId, dialog, date, ct);
    }

    public async Task NoDateAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Date))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnDateChosenAsync(chatId, dialog, date: null, ct);
    }

    // ---------- Time ----------

    /// <summary>"Без часу", on the hour step or the minutes step.</summary>
    public async Task NoTimeAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Hour, EditorStep.Minute))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnTimeChosenAsync(chatId, dialog, time: null, ct);
    }

    // ---------- Repeat ----------

    public async Task ChooseRepeatAsync(long chatId, RepeatChoice choice, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Repeat))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        switch (choice)
        {
            case RepeatChoice.EveryNDays:
                dialog.Step = EditorStep.Interval;
                await RenderAsync(chatId, dialog, error: null, ct);
                break;

            default:
                var repeat = choice switch
                {
                    RepeatChoice.Daily => new RepeatInput(RepeatKind.Daily),
                    RepeatChoice.Monthly => new RepeatInput(RepeatKind.Monthly),
                    RepeatChoice.Yearly => new RepeatInput(RepeatKind.Yearly),
                    _ => null
                };
                await OnRepeatChosenAsync(chatId, dialog, repeat, ct);
                break;
        }
    }

    public async Task ChooseIntervalAsync(long chatId, int days, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Interval) || days is < RepeatRule.MinInterval or > RepeatRule.MaxInterval)
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnRepeatChosenAsync(chatId, dialog, new RepeatInput(RepeatKind.EveryNDays, Interval: days), ct);
    }

    // ---------- Reminder ----------

    public async Task ChooseReminderAsync(long chatId, int minutesBefore, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Reminder)
            || minutesBefore is < 0 or > PlannerTask.MaxRemindMinutesBefore)
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnReminderChosenAsync(chatId, dialog, minutesBefore, ct);
    }

    public Task OtherReminderAsync(long chatId, CancellationToken ct) =>
        MoveAsync(chatId, EditorStep.ReminderHours, ct, EditorStep.Reminder);

    public async Task ChooseReminderHoursAsync(long chatId, int hours, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.ReminderHours)
            || hours is < 1 or > PlannerTask.MaxRemindMinutesBefore / 60)
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnReminderChosenAsync(chatId, dialog, hours * 60, ct);
    }

    public async Task NoReminderAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Reminder, EditorStep.ReminderHours))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnReminderChosenAsync(chatId, dialog, minutesBefore: null, ct);
    }

    // ---------- Topic ----------

    public async Task ChooseTopicAsync(long chatId, Guid topicId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Topic))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnTopicChosenAsync(chatId, dialog, topicId, ct);
    }

    public async Task NoTopicAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, EditorStep.Topic))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        await OnTopicChosenAsync(chatId, dialog, topicId: null, ct);
    }

    public Task NewTopicAsync(long chatId, CancellationToken ct) =>
        MoveAsync(chatId, EditorStep.NewTopicName, ct, EditorStep.Topic);

    public Task BackToTopicsAsync(long chatId, CancellationToken ct) =>
        MoveAsync(chatId, EditorStep.Topic, ct, EditorStep.NewTopicName);

    // ---------- Typed text ----------

    public bool IsEditing(long chatId) => _dialogs.Get<PlannerDialog>(chatId)?.IsEditing == true;

    public async Task HandleTextAsync(long chatId, string? text, CancellationToken ct)
    {
        var dialog = _dialogs.Get<PlannerDialog>(chatId);
        if (dialog?.Step is null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            await RenderAsync(chatId, dialog, PlannerView.NotTextError, ct);
            return;
        }

        text = text.Trim();
        switch (dialog.Step)
        {
            case EditorStep.Title:
                await OnTitleAsync(chatId, dialog, text, ct);
                break;
            case EditorStep.Hour:
                await OnHourAsync(chatId, dialog, text, ct);
                break;
            case EditorStep.Minute:
                await OnMinutesAsync(chatId, dialog, text, ct);
                break;
            // Typing on the topic step is a shortcut for "➕ Нова тема"
            case EditorStep.Topic:
            case EditorStep.NewTopicName:
                await OnNewTopicAsync(chatId, dialog, text, ct);
                break;
            default:
                await RenderAsync(chatId, dialog, PlannerView.ChooseWithButtonsError, ct);
                break;
        }
    }

    private async Task OnTitleAsync(long chatId, PlannerDialog dialog, string title, CancellationToken ct)
    {
        if (title.Length > PlannerTask.MaxTitleLength)
        {
            await RenderAsync(chatId, dialog, PlannerView.TooLongError(PlannerTask.MaxTitleLength), ct);
            return;
        }

        if (dialog.EditedField == TaskField.Title)
        {
            await SaveFieldAsync(chatId, dialog, new RenameTaskCommand(chatId, dialog.TaskId!.Value, title), ct);
            return;
        }

        dialog.Draft.Title = title;
        dialog.Step = EditorStep.Date;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnHourAsync(long chatId, PlannerDialog dialog, string text, CancellationToken ct)
    {
        if (!TryParseNumber(text, max: 23, out var hour))
        {
            await RenderAsync(chatId, dialog, PlannerView.HourError, ct);
            return;
        }

        // Right away the minutes: the hour is shown, the minutes wait under a spoiler
        dialog.Draft.Hour = hour;
        dialog.Step = EditorStep.Minute;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnMinutesAsync(long chatId, PlannerDialog dialog, string text, CancellationToken ct)
    {
        if (dialog.Draft.Hour is not { } hour)
        {
            dialog.Step = EditorStep.Hour;
            await RenderAsync(chatId, dialog, error: null, ct);
            return;
        }

        if (!TryParseNumber(text, max: 59, out var minutes))
        {
            await RenderAsync(chatId, dialog, PlannerView.MinutesError, ct);
            return;
        }

        await OnTimeChosenAsync(chatId, dialog, new TimeOnly(hour, minutes), ct);
    }

    private async Task OnNewTopicAsync(long chatId, PlannerDialog dialog, string name, CancellationToken ct)
    {
        if (name.Length > PlannerTopic.MaxNameLength)
        {
            await RenderAsync(chatId, dialog, PlannerView.TooLongError(PlannerTopic.MaxNameLength), ct);
            return;
        }

        // May return an existing topic written differently, e.g. "дім" for "🏠 Дім"
        var topicId = await _sender.Send(new AddTopicCommand(chatId, name), ct);
        await OnTopicChosenAsync(chatId, dialog, topicId, ct);
    }

    // ---------- A step is done: the next one, or saving the edited field ----------

    private async Task OnDateChosenAsync(long chatId, PlannerDialog dialog, DateOnly? date, CancellationToken ct)
    {
        if (dialog.EditedField == TaskField.Date)
        {
            await SaveFieldAsync(chatId, dialog, new RescheduleTaskCommand(chatId, dialog.TaskId!.Value, date), ct);
            return;
        }

        dialog.Draft.Date = date;

        // Without a date there is no time, repeat or reminder to ask about
        if (date is null)
        {
            await AfterScheduleAsync(chatId, dialog, ct);
            return;
        }

        dialog.Draft.Hour = null;
        dialog.Step = EditorStep.Hour;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnTimeChosenAsync(long chatId, PlannerDialog dialog, TimeOnly? time, CancellationToken ct)
    {
        if (dialog.EditedField == TaskField.Time)
        {
            await SaveFieldAsync(chatId, dialog, new SetTaskTimeCommand(chatId, dialog.TaskId!.Value, time), ct);
            return;
        }

        dialog.Draft.Time = time;
        dialog.Step = EditorStep.Repeat;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnRepeatChosenAsync(long chatId, PlannerDialog dialog, RepeatInput? repeat, CancellationToken ct)
    {
        if (dialog.EditedField == TaskField.Repeat)
        {
            await SaveFieldAsync(chatId, dialog, new SetTaskRepeatCommand(chatId, dialog.TaskId!.Value, repeat), ct);
            return;
        }

        dialog.Draft.Repeat = repeat;

        // Only a task with a time can be reminded about
        if (dialog.Draft.Time is null)
        {
            await AfterScheduleAsync(chatId, dialog, ct);
            return;
        }

        dialog.Step = EditorStep.Reminder;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnReminderChosenAsync(long chatId, PlannerDialog dialog, int? minutesBefore, CancellationToken ct)
    {
        if (dialog.EditedField == TaskField.Reminder)
        {
            await SaveFieldAsync(chatId, dialog, new SetTaskReminderCommand(chatId, dialog.TaskId!.Value, minutesBefore), ct);
            return;
        }

        dialog.Draft.RemindMinutesBefore = minutesBefore;
        await AfterScheduleAsync(chatId, dialog, ct);
    }

    private async Task OnTopicChosenAsync(long chatId, PlannerDialog dialog, Guid? topicId, CancellationToken ct)
    {
        if (dialog.EditedField == TaskField.Topic)
        {
            await SaveFieldAsync(chatId, dialog, new SetTaskTopicCommand(chatId, dialog.TaskId!.Value, topicId), ct);
            return;
        }

        dialog.Draft.TopicId = topicId;
        await SaveNewAsync(chatId, dialog, ct);
    }

    /// <summary>The schedule of a new task is set: the topic is the last question, unless it is already known.</summary>
    private async Task AfterScheduleAsync(long chatId, PlannerDialog dialog, CancellationToken ct)
    {
        if (dialog.Draft.TopicId is not null)
        {
            await SaveNewAsync(chatId, dialog, ct);
            return;
        }

        dialog.Step = EditorStep.Topic;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task SaveNewAsync(long chatId, PlannerDialog dialog, CancellationToken ct)
    {
        var draft = dialog.Draft;
        await _sender.Send(new AddTaskCommand(
            chatId,
            draft.Title!,
            draft.Date,
            draft.Time,
            draft.Repeat,
            draft.RemindMinutesBefore,
            draft.TopicId), ct);

        await _planner.ShowCurrentListAsync(chatId, PlannerView.AddedNotice(draft.Title!), ct);
    }

    private async Task SaveFieldAsync(long chatId, PlannerDialog dialog, IRequest command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        await _planner.ShowCardAsync(chatId, dialog.TaskId!.Value, PlannerView.SavedNotice, ct);
    }

    // ---------- Helpers ----------

    private async Task MoveAsync(long chatId, EditorStep to, CancellationToken ct, params EditorStep[] from)
    {
        if (!TryGetDialog(chatId, out var dialog, from))
        {
            await CancelAsync(chatId, ct);
            return;
        }

        dialog.Step = to;
        await RenderAsync(chatId, dialog, error: null, ct);
    }

    private async Task RenderAsync(long chatId, PlannerDialog dialog, string? error, CancellationToken ct)
    {
        var topics = dialog.Step == EditorStep.Topic
            ? await _sender.Send(new GetTopicsQuery(chatId), ct)
            : null;

        var screen = TaskEditorView.Step(dialog, topics, _time.GetLocalToday(), error);
        await _planner.RenderAsync(chatId, dialog, screen, ct);
    }

    /// <summary>A whole number from 0 to <paramref name="max"/>, e.g. "9" or "09".</summary>
    private static bool TryParseNumber(string text, int max, out int number) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number <= max;

    /// <summary>A button of an old step (or of no editor at all) must not act on the current one.</summary>
    private bool TryGetDialog(long chatId, [NotNullWhen(true)] out PlannerDialog? dialog, params EditorStep[] steps)
    {
        dialog = _dialogs.Get<PlannerDialog>(chatId);
        return dialog?.Step is { } step && steps.Contains(step);
    }
}
