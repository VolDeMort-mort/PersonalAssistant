using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Presentation.Bot.Finance;
using static PersonalAssistant.Presentation.Bot.Finance.FinancePayloads;

namespace PersonalAssistant.Presentation.Bot.Handlers;

public class FinanceCallbackHandler : ICallbackHandler
{
    private readonly FinanceFlow _flow;
    private readonly ILogger<FinanceCallbackHandler> _logger;

    public FinanceCallbackHandler(FinanceFlow flow, ILogger<FinanceCallbackHandler> logger)
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
                { Name: Actions.Balance } => _flow.StartBalanceAsync(chatId, ct),
                { Name: Actions.Templates, Type: { } type } => _flow.ShowTemplatesAsync(chatId, type, notice: null, ct),
                { Name: Actions.Template, Id: { } id } => _flow.ShowTemplateAsync(chatId, id, ct),
                { Name: Actions.UseTemplate, Id: { } id } => _flow.UseTemplateAsync(chatId, id, ct),
                { Name: Actions.DeleteTemplate, Id: { } id } => _flow.DeleteTemplateAsync(chatId, id, ct),
                { Name: Actions.Manual, Type: { } type } => _flow.StartManualEntryAsync(chatId, type, ct),
                { Name: Actions.NewTemplate, Type: { } type } => _flow.StartNewTemplateAsync(chatId, type, ct),
                { Name: Actions.Category, Id: { } id } => _flow.ChooseCategoryAsync(chatId, id, ct),
                { Name: Actions.NewCategory } => _flow.AskNewCategoryAsync(chatId, ct),
                { Name: Actions.BackToCategories } => _flow.BackToCategoriesAsync(chatId, ct),
                { Name: Actions.Skip } => _flow.SkipAsync(chatId, ct),
                { Name: Actions.Cancel } => _flow.CancelAsync(chatId, ct),
                { Name: Actions.Undo, Id: { } id } => _flow.UndoAsync(chatId, id, ct),
                { Name: Actions.History, Number: { } page } => _flow.ShowHistoryAsync(chatId, page, notice: null, ct),
                { Name: Actions.Transaction, TransactionRef: { } tx } => _flow.ShowTransactionAsync(chatId, tx.Id, tx.Page, confirmDelete: false, ct),
                { Name: Actions.DeleteTransaction, TransactionRef: { } tx } => _flow.ShowTransactionAsync(chatId, tx.Id, tx.Page, confirmDelete: true, ct),
                { Name: Actions.ConfirmDeleteTransaction, TransactionRef: { } tx } => _flow.DeleteTransactionAsync(chatId, tx.Id, tx.Page, ct),
                _ => UnknownAsync(chatId, context.Payload, ct)
            });
        }
        catch (NotFoundException ex)
        {
            // A button of something already deleted, e.g. an old template card
            _logger.LogInformation("Finance button points to a missing item: {Reason}", ex.Message);
            await _flow.ShowHomeAsync(chatId, FinanceView.GoneNotice, recordedId: null, ct);
        }
    }

    private Task UnknownAsync(long chatId, string payload, CancellationToken ct)
    {
        _logger.LogWarning("Unknown finance payload {Payload}", payload);
        return _flow.ShowHomeAsync(chatId, ct);
    }
}
