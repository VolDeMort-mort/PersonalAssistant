using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Features.Planner.Commands;
using PersonalAssistant.Presentation.Bot.Planner;
using static PersonalAssistant.Presentation.Bot.Planner.PlannerPayloads;

namespace PersonalAssistant.Presentation.Bot.Handlers;

public class PlannerCallbackHandler : ICallbackHandler
{
    private readonly PlannerFlow _flow;
    private readonly ILogger<PlannerCallbackHandler> _logger;

    public PlannerCallbackHandler(PlannerFlow flow, ILogger<PlannerCallbackHandler> logger)
    {
        _flow = flow;
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
                { Name: Actions.Home } => _flow.OpenAsync(chatId, ct),
                { Name: Actions.Tomorrow } => _flow.ShowTomorrowAsync(chatId, ct),
                { Name: Actions.Day, Date: { } date } => _flow.ShowWeekDayAsync(chatId, date, ct),
                { Name: Actions.Week } => _flow.ShowWeekAsync(chatId, ct),
                { Name: Actions.Topics } => _flow.ShowTopicsAsync(chatId, notice: null, ct),
                { Name: Actions.Topic, Id: { } id } => _flow.ShowTopicAsync(chatId, id, ct),
                { Name: Actions.NewTopic } => _flow.AskNewTopicAsync(chatId, error: null, ct),
                { Name: Actions.DeleteTopic, Id: { } id } => _flow.AskDeleteTopicAsync(chatId, id, ct),
                { Name: Actions.ConfirmDeleteTopic, Id: { } id } => _flow.DeleteTopicAsync(chatId, id, ct),
                { Name: Actions.Page, Number: { } page } => _flow.ChangePageAsync(chatId, page, ct),
                { Name: Actions.Card, Id: { } id } => _flow.ShowCardAsync(chatId, id, notice: null, ct),
                { Name: Actions.Back } => _flow.ShowCurrentListAsync(chatId, notice: null, ct),
                { Name: Actions.Done, Id: { } id } => _flow.CompleteAsync(chatId, id, ct),
                { Name: Actions.Undo, Completion: { } completion } => _flow.UndoAsync(chatId, completion.Id, completion.Date, ct),
                { Name: Actions.Delete, Id: { } id } => _flow.AskDeleteAsync(chatId, id, ct),
                { Name: Actions.ConfirmDelete, Id: { } id } => _flow.DeleteAsync(chatId, id, ct),
                { Name: Actions.Settings } => _flow.ShowSettingsAsync(chatId, notice: null, ct),
                { Name: Actions.AgendaHour, Number: { } hour } => _flow.SetAgendaHourAsync(chatId, hour, ct),
                { Name: Actions.AgendaOff } => _flow.SetAgendaHourAsync(chatId, hour: null, ct),
                { Name: Actions.ReminderDone, Id: { } id } => _flow.CompleteFromReminderAsync(chatId, context.MessageId, id, ct),
                { Name: Actions.ReminderIn15Minutes, Id: { } id } =>
                    _flow.SnoozeFromReminderAsync(chatId, context.MessageId, id, SnoozeOption.FifteenMinutes, ct),
                { Name: Actions.ReminderInHour, Id: { } id } =>
                    _flow.SnoozeFromReminderAsync(chatId, context.MessageId, id, SnoozeOption.OneHour, ct),
                { Name: Actions.ReminderTomorrow, Id: { } id } =>
                    _flow.SnoozeFromReminderAsync(chatId, context.MessageId, id, SnoozeOption.Tomorrow, ct),
                { Name: Actions.ReminderClose } => _flow.CloseReminderAsync(chatId, context.MessageId, ct),
                { Name: Actions.OpenFromAgenda } => _flow.OpenFromAgendaAsync(chatId, ct),
                _ => UnknownAsync(chatId, context.Payload, ct)
            });
        }
        catch (NotFoundException ex)
        {
            // A button of something already deleted or done, e.g. an old reminder
            _logger.LogInformation("Planner button points to a missing item: {Reason}", ex.Message);
            await _flow.ShowHomeAsync(chatId, PlannerView.GoneNotice, ct);
        }
    }

    private Task UnknownAsync(long chatId, string payload, CancellationToken ct)
    {
        _logger.LogWarning("Unknown planner payload {Payload}", payload);
        return _flow.ShowHomeAsync(chatId, notice: null, ct);
    }
}
