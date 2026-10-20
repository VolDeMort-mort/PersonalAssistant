using System.Diagnostics.CodeAnalysis;
using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Features.Finance.Commands;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Features.Finance.Queries;
using PersonalAssistant.Domain.Entities.Finance;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Shopping list scenarios: the list and quick add, an item's card, buying it, and what was bought.
/// The new item wizard and editing a field are in <see cref="ShoppingEditorFlow"/>.
/// </summary>
public class ShoppingFlow
{
    private readonly ISender _sender;
    private readonly IBotMessenger _messenger;
    private readonly IDialogStore _dialogs;
    private readonly IScreenTracker _screens;

    public ShoppingFlow(ISender sender, IBotMessenger messenger, IDialogStore dialogs, IScreenTracker screens)
    {
        _sender = sender;
        _messenger = messenger;
        _dialogs = dialogs;
        _screens = screens;
    }

    // ---------- The list ----------

    /// <param name="recordedId">The expense of an item just bought: the list confirms it and offers undo.</param>
    public async Task ShowListAsync(long chatId, string? notice, CancellationToken ct, Guid? recordedId = null)
    {
        var list = await _sender.Send(new GetShoppingListQuery(chatId), ct);
        var recorded = recordedId is { } id
            ? await _sender.Send(new GetTransactionQuery(chatId, id), ct)
            : null;

        // While the list is shown, typed text becomes new items
        await RenderAsync(chatId, ShoppingDialog.OnList(), ShoppingView.List(list, notice, recorded), ct);
    }

    /// <param name="confirmDelete">Asks "delete it?" instead of offering the actions.</param>
    public async Task ShowItemAsync(long chatId, Guid itemId, string? notice, CancellationToken ct, bool confirmDelete = false)
    {
        _dialogs.Remove(chatId);

        var item = await GetItemAsync(chatId, itemId, ct);
        await ShowAsync(chatId, ShoppingView.Card(item, notice, confirmDelete), ct);
    }

    public async Task DeleteAsync(long chatId, Guid itemId, CancellationToken ct)
    {
        var item = await GetItemAsync(chatId, itemId, ct);
        await _sender.Send(new DeleteShoppingItemCommand(chatId, itemId), ct);
        await ShowListAsync(chatId, ShoppingView.DeletedNotice(item.Title), ct);
    }

    // ---------- Buying ----------

    /// <summary>"✅ Куплено": the category first if the item has none, then the amount.</summary>
    public async Task StartBuyingAsync(long chatId, Guid itemId, CancellationToken ct)
    {
        var item = await GetItemAsync(chatId, itemId, ct);
        await StartDialogAsync(chatId, ShoppingDialog.Buy(item.Id, item.Title, item.CategoryId is not null, item.Price), ct);
    }

    public async Task ChooseBuyCategoryAsync(long chatId, Guid categoryId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, ShoppingStep.BuyCategory))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        await OnBuyCategoryChosenAsync(chatId, dialog, categoryId, ct);
    }

    /// <summary>"✅ 2 500 ₴": paid exactly the item's price.</summary>
    public async Task BuyExpectedAsync(long chatId, Guid itemId, CancellationToken ct)
    {
        // After a restart the chosen category is gone: start buying again rather than guess it
        if (!TryGetDialog(chatId, out var dialog, ShoppingStep.BuyConfirm) || dialog.ItemId != itemId)
        {
            await StartBuyingAsync(chatId, itemId, ct);
            return;
        }

        await BuyAsync(chatId, dialog, amount: null, ct);
    }

    public async Task BuyOtherAsync(long chatId, Guid itemId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, ShoppingStep.BuyConfirm) || dialog.ItemId != itemId)
        {
            await StartBuyingAsync(chatId, itemId, ct);
            return;
        }

        dialog.Step = ShoppingStep.BuyAmount;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    /// <summary>"↩️ Скасувати" after buying: the expense goes, the item is back on the list.</summary>
    public async Task UndoAsync(long chatId, Guid transactionId, CancellationToken ct)
    {
        var transaction = await _sender.Send(new GetTransactionQuery(chatId, transactionId), ct);

        var notice = transaction is not null && await _sender.Send(new DeleteTransactionCommand(chatId, transactionId), ct)
            ? FinanceView.UndoneNotice(transaction)
            : FinanceView.AlreadyUndoneNotice;

        await ShowListAsync(chatId, notice, ct);
    }

    // ---------- Bought ----------

    public async Task ShowBoughtAsync(long chatId, int page, CancellationToken ct)
    {
        _dialogs.Remove(chatId);

        var bought = await _sender.Send(new GetBoughtItemsQuery(chatId, page), ct);
        await ShowAsync(chatId, ShoppingView.Bought(bought, notice: null), ct);
    }

    /// <param name="page">The page of purchases to return to.</param>
    public async Task ShowBoughtItemAsync(long chatId, Guid itemId, int page, CancellationToken ct)
    {
        _dialogs.Remove(chatId);
        await ShowAsync(chatId, ShoppingView.BoughtCard(await GetBoughtItemAsync(chatId, itemId, ct), page), ct);
    }

    public async Task BuyAgainAsync(long chatId, Guid itemId, CancellationToken ct)
    {
        var item = await GetBoughtItemAsync(chatId, itemId, ct);
        await _sender.Send(new RepeatShoppingItemCommand(chatId, itemId), ct);
        await ShowListAsync(chatId, ShoppingView.RepeatedNotice(item.Title), ct);
    }

    // ---------- Shared steps: new category, cancel ----------

    /// <summary>"➕ Нова категорія", on the category step of the wizard or of buying.</summary>
    public async Task AskNewCategoryAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, ShoppingStep.Category, ShoppingStep.BuyCategory))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        dialog.Step = dialog.Step == ShoppingStep.BuyCategory ? ShoppingStep.BuyNewCategoryName : ShoppingStep.NewCategoryName;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    public async Task BackToCategoriesAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, ShoppingStep.NewCategoryName, ShoppingStep.BuyNewCategoryName))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        dialog.Step = dialog.Step == ShoppingStep.BuyNewCategoryName ? ShoppingStep.BuyCategory : ShoppingStep.Category;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    /// <summary>Back to the item when editing or buying it, otherwise to the list.</summary>
    public async Task CancelAsync(long chatId, CancellationToken ct)
    {
        if (_dialogs.Get<ShoppingDialog>(chatId) is { ItemId: { } itemId })
            await ShowItemAsync(chatId, itemId, notice: null, ct);
        else
            await ShowListAsync(chatId, notice: null, ct);
    }

    // ---------- Typed text: quick add and buying ----------

    public bool IsWaitingForText(long chatId) =>
        _dialogs.Get<ShoppingDialog>(chatId) is { } dialog && dialog.ScreenId == _screens.Get(chatId);

    /// <summary>The list or a step of buying is waiting; the wizard and editing are <see cref="ShoppingEditorFlow"/>'s.</summary>
    public bool HandlesText(long chatId) => _dialogs.Get<ShoppingDialog>(chatId) is { IsOnList: true } or { IsBuying: true };

    public async Task HandleTextAsync(long chatId, string? text, CancellationToken ct)
    {
        var dialog = _dialogs.Get<ShoppingDialog>(chatId);
        if (dialog is null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (dialog.IsOnList)
                await ShowListAsync(chatId, FinanceView.NotTextError, ct);
            else
                await RenderDialogAsync(chatId, dialog, FinanceView.NotTextError, ct);
            return;
        }

        text = text.Trim();
        switch (dialog.Step)
        {
            case null:
                await QuickAddAsync(chatId, text, ct);
                break;
            // Typing on the category step is a shortcut for "➕ Нова категорія"
            case ShoppingStep.BuyCategory:
            case ShoppingStep.BuyNewCategoryName:
                await OnBuyNewCategoryAsync(chatId, dialog, text, ct);
                break;
            // Typing an amount on the confirm step is a shortcut for "✏️ Інша сума"
            case ShoppingStep.BuyConfirm:
            case ShoppingStep.BuyAmount:
                await OnBuyAmountAsync(chatId, dialog, text, ct);
                break;
            default:
                await RenderDialogAsync(chatId, dialog, ShoppingView.ChooseWithButtonsError, ct);
                break;
        }
    }

    private async Task QuickAddAsync(long chatId, string text, CancellationToken ct)
    {
        var items = ShoppingText.ReadItems(text);
        if (items.Count == 0)
        {
            await ShowListAsync(chatId, FinanceView.NotTextError, ct);
            return;
        }

        await _sender.Send(new AddShoppingItemsCommand(chatId, items), ct);
        await ShowListAsync(chatId, ShoppingView.AddedNotice(items.Select(i => i.Title).ToList()), ct);
    }

    private async Task OnBuyNewCategoryAsync(long chatId, ShoppingDialog dialog, string name, CancellationToken ct)
    {
        if (name.Length > FinanceCategory.MaxNameLength)
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(FinanceCategory.MaxNameLength), ct);
            return;
        }

        // May return an existing category written differently, e.g. "одяг" for "👕 Одяг"
        var categoryId = await _sender.Send(new AddCategoryCommand(chatId, TransactionType.Expense, name), ct);
        await OnBuyCategoryChosenAsync(chatId, dialog, categoryId, ct);
    }

    private async Task OnBuyAmountAsync(long chatId, ShoppingDialog dialog, string text, CancellationToken ct)
    {
        if (!MoneyFormat.TryParse(text, allowZeroOrNegative: false, out var amount))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.AmountError, ct);
            return;
        }

        await BuyAsync(chatId, dialog, amount, ct);
    }

    private async Task OnBuyCategoryChosenAsync(long chatId, ShoppingDialog dialog, Guid categoryId, CancellationToken ct)
    {
        dialog.BuyCategoryId = categoryId;
        dialog.Step = dialog.ExpectedPrice is null ? ShoppingStep.BuyAmount : ShoppingStep.BuyConfirm;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    /// <param name="amount">Null pays the item's price.</param>
    private async Task BuyAsync(long chatId, ShoppingDialog dialog, long? amount, CancellationToken ct)
    {
        var transactionId = await _sender.Send(
            new BuyShoppingItemCommand(chatId, dialog.ItemId!.Value, amount, dialog.BuyCategoryId), ct);

        await ShowListAsync(chatId, notice: null, ct, transactionId);
    }

    // ---------- Shared with the editor ----------

    public async Task StartDialogAsync(long chatId, ShoppingDialog dialog, CancellationToken ct)
    {
        _dialogs.Set(chatId, dialog);
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    public async Task RenderDialogAsync(long chatId, ShoppingDialog dialog, string? error, CancellationToken ct)
    {
        var categories = dialog.Step is ShoppingStep.Category or ShoppingStep.BuyCategory
            ? await _sender.Send(new GetCategoriesQuery(chatId, TransactionType.Expense), ct)
            : null;

        await RenderAsync(chatId, dialog, ShoppingView.Dialog(dialog, categories, error), ct);
    }

    /// <summary>A button of an old step (or of no dialog at all) must not act on the current one.</summary>
    public bool TryGetDialog(long chatId, [NotNullWhen(true)] out ShoppingDialog? dialog, params ShoppingStep[] steps)
    {
        dialog = _dialogs.Get<ShoppingDialog>(chatId);
        return dialog?.Step is { } step && steps.Contains(step);
    }

    public async Task<ShoppingItemDto> GetItemAsync(long chatId, Guid itemId, CancellationToken ct) =>
        await _sender.Send(new GetShoppingItemQuery(chatId, itemId), ct)
        ?? throw new NotFoundException(nameof(ShoppingItem), itemId);

    // ---------- Helpers ----------

    private async Task<BoughtItemDto> GetBoughtItemAsync(long chatId, Guid itemId, CancellationToken ct) =>
        await _sender.Send(new GetBoughtItemQuery(chatId, itemId), ct)
        ?? throw new NotFoundException(nameof(ShoppingItem), itemId);

    private async Task RenderAsync(long chatId, ShoppingDialog dialog, BotScreen screen, CancellationToken ct)
    {
        _dialogs.Set(chatId, dialog);
        await ShowAsync(chatId, screen, ct);

        // The window may have been re-sent as a new message: listen to the current one
        dialog.ScreenId = _screens.Get(chatId);
    }

    private Task ShowAsync(long chatId, BotScreen screen, CancellationToken ct) =>
        _messenger.ShowScreenAsync(chatId, screen.Text, screen.Keyboard, ct);
}
