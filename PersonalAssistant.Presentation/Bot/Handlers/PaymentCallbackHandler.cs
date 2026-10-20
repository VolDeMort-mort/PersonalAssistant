using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Presentation.Bot.Finance;
using static PersonalAssistant.Presentation.Bot.Finance.PaymentPayloads;

namespace PersonalAssistant.Presentation.Bot.Handlers;

public class PaymentCallbackHandler : ICallbackHandler
{
    private readonly PaymentFlow _flow;
    private readonly ILogger<PaymentCallbackHandler> _logger;

    public PaymentCallbackHandler(PaymentFlow flow, ILogger<PaymentCallbackHandler> logger)
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
                { Name: Actions.List } => _flow.ShowListAsync(chatId, notice: null, ct),
                { Name: Actions.Card, PaymentRef: { } payment } => _flow.ShowCardAsync(chatId, payment.Id, payment.FromDay, ct),
                { Name: Actions.Paid, PaymentRef: { } payment } => _flow.AskPaidAmountAsync(chatId, payment.Id, payment.FromDay, ct),
                { Name: Actions.PayExpected, PaymentRef: { } payment } =>
                    _flow.PayAsync(chatId, payment.Id, amount: null, payment.FromDay, ct),
                { Name: Actions.PayOther, PaymentRef: { } payment } => _flow.StartOtherAmountAsync(chatId, payment.Id, payment.FromDay, ct),
                { Name: Actions.Delete, PaymentRef: { } payment } => _flow.DeleteAsync(chatId, payment.Id, payment.FromDay, ct),
                { Name: Actions.New } => _flow.StartNewAsync(chatId, ct),
                { Name: Actions.Category, Id: { } id } => _flow.ChooseCategoryAsync(chatId, id, ct),
                { Name: Actions.NewCategory } => _flow.AskNewCategoryAsync(chatId, ct),
                { Name: Actions.BackToCategories } => _flow.BackToCategoriesAsync(chatId, ct),
                { Name: Actions.Recurrence, Recurrence: { } recurrence } => _flow.ChooseRecurrenceAsync(chatId, recurrence, ct),
                { Name: Actions.Weekday, Number: { } day } => _flow.ChooseWeekdayAsync(chatId, day, ct),
                { Name: Actions.MonthDay, Number: { } day } => _flow.ChooseMonthDayAsync(chatId, day, ct),
                { Name: Actions.Month, Number: { } yearMonth } => _flow.ChooseMonthAsync(chatId, yearMonth, ct),
                { Name: Actions.Day, Number: { } day } => _flow.ChooseDayAsync(chatId, day, ct),
                { Name: Actions.RemindDays, Number: { } days } => _flow.ChooseRemindDaysAsync(chatId, days, ct),
                { Name: Actions.NoReminder } => _flow.ChooseRemindDaysAsync(chatId, daysBefore: null, ct),
                { Name: Actions.RemindHour, Number: { } hour } => _flow.ChooseRemindHourAsync(chatId, hour, ct),
                { Name: Actions.Cancel } => _flow.CancelAsync(chatId, ct),
                { Name: Actions.ReminderPaid, Id: { } id } => _flow.PayFromReminderAsync(chatId, context.MessageId, id, ct),
                { Name: Actions.ReminderClose } => _flow.CloseReminderAsync(chatId, context.MessageId, ct),
                _ => UnknownAsync(chatId, context.Payload, ct)
            });
        }
        catch (NotFoundException ex)
        {
            // A button of something already deleted or paid, e.g. an old reminder
            _logger.LogInformation("Payment button points to a missing item: {Reason}", ex.Message);
            await _flow.ShowListAsync(chatId, FinanceView.GoneNotice, ct);
        }
    }

    private Task UnknownAsync(long chatId, string payload, CancellationToken ct)
    {
        _logger.LogWarning("Unknown payment payload {Payload}", payload);
        return _flow.ShowListAsync(chatId, notice: null, ct);
    }
}
