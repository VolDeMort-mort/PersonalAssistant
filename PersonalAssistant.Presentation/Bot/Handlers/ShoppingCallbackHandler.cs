using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Presentation.Bot.Finance;
using static PersonalAssistant.Presentation.Bot.Finance.ShoppingPayloads;

namespace PersonalAssistant.Presentation.Bot.Handlers;

public class ShoppingCallbackHandler : ICallbackHandler
{
    private readonly ShoppingFlow _flow;
    private readonly ShoppingEditorFlow _editor;
    private readonly ILogger<ShoppingCallbackHandler> _logger;

    public ShoppingCallbackHandler(ShoppingFlow flow, ShoppingEditorFlow editor, ILogger<ShoppingCallbackHandler> logger)
    {
        _flow = flow;
        _editor = editor;
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
                { Name: Actions.Item, Id: { } id } => _flow.ShowItemAsync(chatId, id, notice: null, ct),
                { Name: Actions.Delete, Id: { } id } => _flow.ShowItemAsync(chatId, id, notice: null, ct, confirmDelete: true),
                { Name: Actions.ConfirmDelete, Id: { } id } => _flow.DeleteAsync(chatId, id, ct),
                { Name: Actions.Buy, Id: { } id } => _flow.StartBuyingAsync(chatId, id, ct),
                { Name: Actions.BuyCategory, Id: { } categoryId } => _flow.ChooseBuyCategoryAsync(chatId, categoryId, ct),
                { Name: Actions.BuyExpected, Id: { } id } => _flow.BuyExpectedAsync(chatId, id, ct),
                { Name: Actions.BuyOther, Id: { } id } => _flow.BuyOtherAsync(chatId, id, ct),
                { Name: Actions.Undo, Id: { } transactionId } => _flow.UndoAsync(chatId, transactionId, ct),
                { Name: Actions.New } => _editor.StartNewAsync(chatId, ct),
                { Name: Actions.EditTitle, Id: { } id } => _editor.StartEditAsync(chatId, id, ShoppingField.Title, ct),
                { Name: Actions.EditPrice, Id: { } id } => _editor.StartEditAsync(chatId, id, ShoppingField.Price, ct),
                { Name: Actions.EditLink, Id: { } id } => _editor.StartEditAsync(chatId, id, ShoppingField.Link, ct),
                { Name: Actions.EditCategory, Id: { } id } => _editor.StartEditAsync(chatId, id, ShoppingField.Category, ct),
                { Name: Actions.Skip } => _editor.SkipAsync(chatId, ct),
                { Name: Actions.Category, Id: { } categoryId } => _editor.ChooseCategoryAsync(chatId, categoryId, ct),
                { Name: Actions.NoCategory } => _editor.NoCategoryAsync(chatId, ct),
                { Name: Actions.NewCategory } => _flow.AskNewCategoryAsync(chatId, ct),
                { Name: Actions.BackToCategories } => _flow.BackToCategoriesAsync(chatId, ct),
                { Name: Actions.Cancel } => _flow.CancelAsync(chatId, ct),
                { Name: Actions.Bought, Number: { } page } => _flow.ShowBoughtAsync(chatId, page, ct),
                { Name: Actions.BoughtItem, BoughtRef: { } bought } => _flow.ShowBoughtItemAsync(chatId, bought.Id, bought.Page, ct),
                { Name: Actions.BuyAgain, Id: { } id } => _flow.BuyAgainAsync(chatId, id, ct),
                _ => UnknownAsync(chatId, context.Payload, ct)
            });
        }
        catch (NotFoundException ex)
        {
            // A button of something already deleted or bought, e.g. an old card
            _logger.LogInformation("Shopping button points to a missing item: {Reason}", ex.Message);
            await _flow.ShowListAsync(chatId, FinanceView.GoneNotice, ct);
        }
    }

    private Task UnknownAsync(long chatId, string payload, CancellationToken ct)
    {
        _logger.LogWarning("Unknown shopping payload {Payload}", payload);
        return _flow.ShowListAsync(chatId, notice: null, ct);
    }
}
