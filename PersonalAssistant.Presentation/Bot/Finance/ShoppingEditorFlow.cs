using MediatR;
using PersonalAssistant.Application.Features.Finance.Commands;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;
using PersonalAssistant.Presentation.Helpers;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// The new item wizard (title → price → link → category) and editing one field from the card.
/// Both walk the same steps; the wizard goes on to the next one, editing saves that field with its own command.
/// </summary>
public class ShoppingEditorFlow
{
    private readonly ISender _sender;
    private readonly IDialogStore _dialogs;
    private readonly ShoppingFlow _flow;

    public ShoppingEditorFlow(ISender sender, IDialogStore dialogs, ShoppingFlow flow)
    {
        _sender = sender;
        _dialogs = dialogs;
        _flow = flow;
    }

    // ---------- Start ----------

    public Task StartNewAsync(long chatId, CancellationToken ct) =>
        _flow.StartDialogAsync(chatId, ShoppingDialog.NewItem(), ct);

    /// <summary>A button of the card: edits one field of the item.</summary>
    public async Task StartEditAsync(long chatId, Guid itemId, ShoppingField field, CancellationToken ct)
    {
        var item = await _flow.GetItemAsync(chatId, itemId, ct);
        await _flow.StartDialogAsync(chatId, ShoppingDialog.Edit(item.Id, item.Title, field), ct);
    }

    // ---------- Buttons ----------

    /// <summary>"⏭ Пропустити" in the wizard, "🗑 Без ціни" / "🗑 Без лінка" when editing.</summary>
    public async Task SkipAsync(long chatId, CancellationToken ct)
    {
        if (!_flow.TryGetDialog(chatId, out var dialog, ShoppingStep.Price, ShoppingStep.Link))
        {
            await _flow.CancelAsync(chatId, ct);
            return;
        }

        if (dialog.Step == ShoppingStep.Price)
            await OnPriceAsync(chatId, dialog, price: null, ct);
        else
            await OnLinkAsync(chatId, dialog, link: null, ct);
    }

    public async Task ChooseCategoryAsync(long chatId, Guid categoryId, CancellationToken ct)
    {
        if (!_flow.TryGetDialog(chatId, out var dialog, ShoppingStep.Category))
        {
            await _flow.CancelAsync(chatId, ct);
            return;
        }

        await OnCategoryAsync(chatId, dialog, categoryId, ct);
    }

    public async Task NoCategoryAsync(long chatId, CancellationToken ct)
    {
        if (!_flow.TryGetDialog(chatId, out var dialog, ShoppingStep.Category))
        {
            await _flow.CancelAsync(chatId, ct);
            return;
        }

        await OnCategoryAsync(chatId, dialog, categoryId: null, ct);
    }

    // ---------- Typed text ----------

    public async Task HandleTextAsync(long chatId, string? text, CancellationToken ct)
    {
        var dialog = _dialogs.Get<ShoppingDialog>(chatId);
        if (dialog?.Step is null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            await _flow.RenderDialogAsync(chatId, dialog, FinanceView.NotTextError, ct);
            return;
        }

        text = text.Trim();
        switch (dialog.Step)
        {
            case ShoppingStep.Title:
                await OnTitleAsync(chatId, dialog, text, ct);
                break;

            case ShoppingStep.Price:
                if (MoneyFormat.TryParse(text, allowZeroOrNegative: false, out var price))
                    await OnPriceAsync(chatId, dialog, price, ct);
                else
                    await _flow.RenderDialogAsync(chatId, dialog, FinanceView.AmountError, ct);
                break;

            // The link may come inside a shared message with other text around it
            case ShoppingStep.Link:
                if (ShoppingText.FindLink(text) is { } link)
                    await OnLinkAsync(chatId, dialog, link, ct);
                else
                    await _flow.RenderDialogAsync(chatId, dialog, ShoppingView.LinkError, ct);
                break;

            // Typing on the category step is a shortcut for "➕ Нова категорія"
            case ShoppingStep.Category:
            case ShoppingStep.NewCategoryName:
                await OnNewCategoryAsync(chatId, dialog, text, ct);
                break;

            default:
                await _flow.RenderDialogAsync(chatId, dialog, ShoppingView.ChooseWithButtonsError, ct);
                break;
        }
    }

    // ---------- A step is done: the next one, or saving the edited field ----------

    private async Task OnTitleAsync(long chatId, ShoppingDialog dialog, string title, CancellationToken ct)
    {
        if (title.Length > ShoppingItem.MaxTitleLength)
        {
            await _flow.RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(ShoppingItem.MaxTitleLength), ct);
            return;
        }

        if (dialog.EditedField == ShoppingField.Title)
        {
            await SaveFieldAsync(chatId, dialog, new RenameShoppingItemCommand(chatId, dialog.ItemId!.Value, title), ct);
            return;
        }

        dialog.Title = title;
        dialog.Step = ShoppingStep.Price;
        await _flow.RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnPriceAsync(long chatId, ShoppingDialog dialog, long? price, CancellationToken ct)
    {
        if (dialog.EditedField == ShoppingField.Price)
        {
            await SaveFieldAsync(chatId, dialog, new SetShoppingItemPriceCommand(chatId, dialog.ItemId!.Value, price), ct);
            return;
        }

        dialog.Price = price;
        dialog.Step = ShoppingStep.Link;
        await _flow.RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnLinkAsync(long chatId, ShoppingDialog dialog, string? link, CancellationToken ct)
    {
        if (dialog.EditedField == ShoppingField.Link)
        {
            await SaveFieldAsync(chatId, dialog, new SetShoppingItemLinkCommand(chatId, dialog.ItemId!.Value, link), ct);
            return;
        }

        dialog.Link = link;
        dialog.Step = ShoppingStep.Category;
        await _flow.RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnNewCategoryAsync(long chatId, ShoppingDialog dialog, string name, CancellationToken ct)
    {
        if (name.Length > FinanceCategory.MaxNameLength)
        {
            await _flow.RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(FinanceCategory.MaxNameLength), ct);
            return;
        }

        // May return an existing category written differently, e.g. "одяг" for "👕 Одяг"
        var categoryId = await _sender.Send(new AddCategoryCommand(chatId, TransactionType.Expense, name), ct);
        await OnCategoryAsync(chatId, dialog, categoryId, ct);
    }

    private async Task OnCategoryAsync(long chatId, ShoppingDialog dialog, Guid? categoryId, CancellationToken ct)
    {
        if (dialog.EditedField == ShoppingField.Category)
        {
            await SaveFieldAsync(chatId, dialog, new SetShoppingItemCategoryCommand(chatId, dialog.ItemId!.Value, categoryId), ct);
            return;
        }

        var item = new NewShoppingItem(dialog.Title!, dialog.Price, dialog.Link, categoryId);
        await _sender.Send(new AddShoppingItemsCommand(chatId, new[] { item }), ct);
        await _flow.ShowListAsync(chatId, ShoppingView.AddedNotice(new[] { item.Title }), ct);
    }

    private async Task SaveFieldAsync(long chatId, ShoppingDialog dialog, IRequest command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        await _flow.ShowItemAsync(chatId, dialog.ItemId!.Value, ShoppingView.SavedNotice, ct);
    }
}
