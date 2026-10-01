using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Presentation.Bot.Planner;
using static PersonalAssistant.Presentation.Bot.Planner.TaskEditorPayloads;

namespace PersonalAssistant.Presentation.Bot.Handlers;

public class TaskEditorCallbackHandler : ICallbackHandler
{
    private readonly TaskEditorFlow _flow;
    private readonly PlannerFlow _planner;
    private readonly ILogger<TaskEditorCallbackHandler> _logger;

    public TaskEditorCallbackHandler(TaskEditorFlow flow, PlannerFlow planner, ILogger<TaskEditorCallbackHandler> logger)
    {
        _flow = flow;
        _planner = planner;
        _logger = logger;
    }

    public bool CanHandle(string payload) => payload.StartsWith(Prefix, StringComparison.Ordinal);

    public async Task HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var chatId = context.ChatId;
        var action = Parse(context.Payload);

        try
        {
            await (action switch
            {
                { Name: Actions.New } => _flow.StartNewAsync(chatId, ct),
                { Name: Actions.EditTitle, Id: { } id } => _flow.StartEditAsync(chatId, id, TaskField.Title, ct),
                { Name: Actions.EditDate, Id: { } id } => _flow.StartEditAsync(chatId, id, TaskField.Date, ct),
                { Name: Actions.EditTime, Id: { } id } => _flow.StartEditAsync(chatId, id, TaskField.Time, ct),
                { Name: Actions.EditRepeat, Id: { } id } => _flow.StartEditAsync(chatId, id, TaskField.Repeat, ct),
                { Name: Actions.EditReminder, Id: { } id } => _flow.StartEditAsync(chatId, id, TaskField.Reminder, ct),
                { Name: Actions.EditTopic, Id: { } id } => _flow.StartEditAsync(chatId, id, TaskField.Topic, ct),
                { Name: Actions.Date, Date: { } date } => _flow.ChooseDateAsync(chatId, date, ct),
                { Name: Actions.OtherDate } => _flow.OtherDateAsync(chatId, ct),
                { Name: Actions.NoDate } => _flow.NoDateAsync(chatId, ct),
                { Name: Actions.Month, Number: { } yearMonth } => _flow.ChooseMonthAsync(chatId, yearMonth, ct),
                { Name: Actions.Day, Number: { } day } => _flow.ChooseDayAsync(chatId, day, ct),
                { Name: Actions.BackToDates } => _flow.BackToDatesAsync(chatId, ct),
                { Name: Actions.Hour, Number: { } hour } => _flow.ChooseHourAsync(chatId, hour, ct),
                { Name: Actions.Minute, Number: { } minute } => _flow.ChooseMinuteAsync(chatId, minute, ct),
                { Name: Actions.NoTime } => _flow.NoTimeAsync(chatId, ct),
                { Name: Actions.Repeat, Repeat: { } choice } => _flow.ChooseRepeatAsync(chatId, choice, ct),
                { Name: Actions.Weekday, Number: { } day } => _flow.ToggleWeekdayAsync(chatId, day, ct),
                { Name: Actions.WeekdaysDone } => _flow.WeekdaysDoneAsync(chatId, ct),
                { Name: Actions.Interval, Number: { } days } => _flow.ChooseIntervalAsync(chatId, days, ct),
                { Name: Actions.Reminder, Number: { } minutes } => _flow.ChooseReminderAsync(chatId, minutes, ct),
                { Name: Actions.OtherReminder } => _flow.OtherReminderAsync(chatId, ct),
                { Name: Actions.ReminderHours, Number: { } hours } => _flow.ChooseReminderHoursAsync(chatId, hours, ct),
                { Name: Actions.NoReminder } => _flow.NoReminderAsync(chatId, ct),
                { Name: Actions.Topic, Id: { } id } => _flow.ChooseTopicAsync(chatId, id, ct),
                { Name: Actions.NoTopic } => _flow.NoTopicAsync(chatId, ct),
                { Name: Actions.NewTopic } => _flow.NewTopicAsync(chatId, ct),
                { Name: Actions.BackToTopics } => _flow.BackToTopicsAsync(chatId, ct),
                { Name: Actions.Cancel } => _flow.CancelAsync(chatId, ct),
                _ => UnknownAsync(chatId, context.Payload, ct)
            });
        }
        catch (NotFoundException ex)
        {
            // The task or the topic was deleted while the editor was open
            _logger.LogInformation("Task editor button points to a missing item: {Reason}", ex.Message);
            await _planner.ShowHomeAsync(chatId, PlannerView.GoneNotice, ct);
        }
    }

    private Task UnknownAsync(long chatId, string payload, CancellationToken ct)
    {
        _logger.LogWarning("Unknown task editor payload {Payload}", payload);
        return _flow.CancelAsync(chatId, ct);
    }
}
