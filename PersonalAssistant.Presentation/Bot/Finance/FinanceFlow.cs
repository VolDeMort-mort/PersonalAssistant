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
/// Finance scenarios as the user walks through them: which command or query to send
/// and which screen comes next. Handlers only translate buttons and messages into these calls.
/// </summary>
public class FinanceFlow
{
    private readonly ISender _sender;
    private readonly IBotMessenger _messenger;
    private readonly IFinanceDialogStore _dialogs;
    private readonly IScreenTracker _screens;

    public FinanceFlow(ISender sender, IBotMessenger messenger, IFinanceDialogStore dialogs, IScreenTracker screens)
    {
        _sender = sender;
        _messenger = messenger;
        _dialogs = dialogs;
        _screens = screens;
    }

    // ---------- Screens ----------

    /// <summary>Entry from the main menu: the very first visit asks for the starting balance.</summary>
    public async Task OpenAsync(long chatId, CancellationToken ct)
    {
        if (await _sender.Send(new InitializeFinanceCommand(chatId), ct))
            await StartDialogAsync(chatId, FinanceDialog.Balance(isFirstVisit: true, knownBalance: 0), ct);
        else
            await ShowHomeAsync(chatId, ct);
    }

    public Task ShowHomeAsync(long chatId, CancellationToken ct) =>
        ShowHomeAsync(chatId, notice: null, recordedId: null, ct);

    /// <param name="recordedId">Transaction that was just recorded: the screen confirms it and offers undo.</param>
    public async Task ShowHomeAsync(long chatId, string? notice, Guid? recordedId, CancellationToken ct)
    {
        _dialogs.Remove(chatId);

        var dashboard = await _sender.Send(new GetFinanceDashboardQuery(chatId), ct);
        var recorded = recordedId is { } id
            ? await _sender.Send(new GetTransactionQuery(chatId, id), ct)
            : null;

        await ShowAsync(chatId, FinanceView.Dashboard(dashboard, notice, recorded), ct);
    }

    public async Task ShowTemplatesAsync(long chatId, TransactionType type, string? notice, CancellationToken ct)
    {
        _dialogs.Remove(chatId);

        var templates = await _sender.Send(new GetTemplatesQuery(chatId, type), ct);
        await ShowAsync(chatId, FinanceView.Templates(type, templates, notice), ct);
    }

    public async Task ShowTemplateAsync(long chatId, Guid templateId, CancellationToken ct)
    {
        _dialogs.Remove(chatId);

        var template = await GetTemplateAsync(chatId, templateId, ct);
        await ShowAsync(chatId, FinanceView.TemplateCard(template), ct);
    }

    // ---------- One-click actions ----------

    public async Task UseTemplateAsync(long chatId, Guid templateId, CancellationToken ct)
    {
        var template = await GetTemplateAsync(chatId, templateId, ct);
        if (template.Amount is null)
        {
            await StartDialogAsync(chatId, FinanceDialog.TemplateUse(template), ct);
            return;
        }

        var transactionId = await _sender.Send(new ApplyTemplateCommand(chatId, templateId), ct);
        await ShowHomeAsync(chatId, notice: null, transactionId, ct);
    }

    public async Task DeleteTemplateAsync(long chatId, Guid templateId, CancellationToken ct)
    {
        var template = await GetTemplateAsync(chatId, templateId, ct);
        await _sender.Send(new DeleteTemplateCommand(chatId, templateId), ct);
        await ShowTemplatesAsync(chatId, template.Type, FinanceView.TemplateDeletedNotice(template.Title), ct);
    }

    public async Task UndoAsync(long chatId, Guid transactionId, CancellationToken ct)
    {
        var transaction = await _sender.Send(new GetTransactionQuery(chatId, transactionId), ct);

        var notice = transaction is not null && await _sender.Send(new DeleteTransactionCommand(chatId, transactionId), ct)
            ? FinanceView.UndoneNotice(transaction)
            : FinanceView.AlreadyUndoneNotice;

        await ShowHomeAsync(chatId, notice, recordedId: null, ct);
    }

    // ---------- Dialogs: start ----------

    public Task StartManualEntryAsync(long chatId, TransactionType type, CancellationToken ct) =>
        StartDialogAsync(chatId, FinanceDialog.ManualEntry(type), ct);

    public Task StartNewTemplateAsync(long chatId, TransactionType type, CancellationToken ct) =>
        StartDialogAsync(chatId, FinanceDialog.NewTemplate(type), ct);

    public async Task StartBalanceAsync(long chatId, CancellationToken ct)
    {
        var dashboard = await _sender.Send(new GetFinanceDashboardQuery(chatId), ct);
        await StartDialogAsync(chatId, FinanceDialog.Balance(isFirstVisit: false, dashboard.Balance), ct);
    }

    // ---------- Dialogs: buttons ----------

    public async Task ChooseCategoryAsync(long chatId, Guid categoryId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, FinanceStep.Category))
        {
            await ShowHomeAsync(chatId, ct);
            return;
        }

        var categories = await _sender.Send(new GetCategoriesQuery(chatId, dialog.Type), ct);
        var category = categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new NotFoundException(nameof(FinanceCategory), categoryId);

        await OnCategoryChosenAsync(chatId, dialog, category, ct);
    }

    public Task AskNewCategoryAsync(long chatId, CancellationToken ct) =>
        MoveToStepAsync(chatId, from: FinanceStep.Category, to: FinanceStep.NewCategoryName, ct);

    public Task BackToCategoriesAsync(long chatId, CancellationToken ct) =>
        MoveToStepAsync(chatId, from: FinanceStep.NewCategoryName, to: FinanceStep.Category, ct);

    /// <summary>"Skip" on the comment step, "ask every time" on the template amount step.</summary>
    public async Task SkipAsync(long chatId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, FinanceStep.Comment, FinanceStep.TemplateAmount))
        {
            await ShowHomeAsync(chatId, ct);
            return;
        }

        if (dialog.Step == FinanceStep.Comment)
            await SaveTransactionAsync(chatId, dialog, comment: null, ct);
        else
            await SaveTemplateAsync(chatId, dialog, amount: null, ct);
    }

    public async Task CancelAsync(long chatId, CancellationToken ct)
    {
        var dialog = _dialogs.Get(chatId);
        if (dialog is null || dialog.Kind == FinanceDialogKind.Balance)
            await ShowHomeAsync(chatId, ct);
        else
            await ShowTemplatesAsync(chatId, dialog.Type, notice: null, ct);
    }

    // ---------- Dialogs: typed text ----------

    public bool IsWaitingForText(long chatId) =>
        _dialogs.Get(chatId) is { } dialog && dialog.ScreenId == _screens.Get(chatId);

    public async Task HandleTextAsync(long chatId, string? text, CancellationToken ct)
    {
        var dialog = _dialogs.Get(chatId);
        if (dialog is null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.NotTextError, ct);
            return;
        }

        text = text.Trim();
        switch (dialog.Step)
        {
            case FinanceStep.Amount:
                await OnAmountAsync(chatId, dialog, text, ct);
                break;
            case FinanceStep.Title:
                await OnTitleAsync(chatId, dialog, text, ct);
                break;
            // Typing on the category step is a shortcut for "➕ Нова категорія"
            case FinanceStep.Category:
            case FinanceStep.NewCategoryName:
                await OnNewCategoryAsync(chatId, dialog, text, ct);
                break;
            case FinanceStep.Comment:
                await OnCommentAsync(chatId, dialog, text, ct);
                break;
            case FinanceStep.TemplateAmount:
                await OnTemplateAmountAsync(chatId, dialog, text, ct);
                break;
            case FinanceStep.Balance:
                await OnBalanceAsync(chatId, dialog, text, ct);
                break;
        }
    }

    private async Task OnAmountAsync(long chatId, FinanceDialog dialog, string text, CancellationToken ct)
    {
        if (!MoneyFormat.TryParse(text, allowZeroOrNegative: false, out var amount))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.AmountError, ct);
            return;
        }

        if (dialog.Kind == FinanceDialogKind.TemplateUse)
        {
            var transactionId = await _sender.Send(new ApplyTemplateCommand(chatId, dialog.TemplateId!.Value, amount), ct);
            await ShowHomeAsync(chatId, notice: null, transactionId, ct);
            return;
        }

        dialog.Amount = amount;
        dialog.Step = FinanceStep.Category;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnTitleAsync(long chatId, FinanceDialog dialog, string text, CancellationToken ct)
    {
        if (text.Length > FinanceTemplate.MaxTitleLength)
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(FinanceTemplate.MaxTitleLength), ct);
            return;
        }

        dialog.Title = text;
        dialog.Step = FinanceStep.Category;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnNewCategoryAsync(long chatId, FinanceDialog dialog, string text, CancellationToken ct)
    {
        if (text.Length > FinanceCategory.MaxNameLength)
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(FinanceCategory.MaxNameLength), ct);
            return;
        }

        var categoryId = await _sender.Send(new AddCategoryCommand(chatId, dialog.Type, text), ct);

        // The command may return an existing category written differently, e.g. "їжа" for "🍔 Їжа"
        var categories = await _sender.Send(new GetCategoriesQuery(chatId, dialog.Type), ct);
        await OnCategoryChosenAsync(chatId, dialog, categories.First(c => c.Id == categoryId), ct);
    }

    private async Task OnCategoryChosenAsync(long chatId, FinanceDialog dialog, CategoryDto category, CancellationToken ct)
    {
        dialog.CategoryId = category.Id;
        dialog.CategoryName = category.Name;
        dialog.Step = dialog.Kind == FinanceDialogKind.NewTemplate ? FinanceStep.TemplateAmount : FinanceStep.Comment;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnCommentAsync(long chatId, FinanceDialog dialog, string text, CancellationToken ct)
    {
        if (text.Length > FinanceTransaction.MaxCommentLength)
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(FinanceTransaction.MaxCommentLength), ct);
            return;
        }

        await SaveTransactionAsync(chatId, dialog, text, ct);
    }

    private async Task OnTemplateAmountAsync(long chatId, FinanceDialog dialog, string text, CancellationToken ct)
    {
        if (!MoneyFormat.TryParse(text, allowZeroOrNegative: false, out var amount))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.AmountError, ct);
            return;
        }

        await SaveTemplateAsync(chatId, dialog, amount, ct);
    }

    private async Task OnBalanceAsync(long chatId, FinanceDialog dialog, string text, CancellationToken ct)
    {
        if (!MoneyFormat.TryParse(text, allowZeroOrNegative: true, out var balance))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.BalanceError, ct);
            return;
        }

        var adjustmentId = await _sender.Send(new SetBalanceCommand(chatId, balance), ct);
        var notice = adjustmentId is null && !dialog.IsFirstVisit ? FinanceView.BalanceUnchangedNotice : null;
        await ShowHomeAsync(chatId, notice, adjustmentId, ct);
    }

    private async Task SaveTransactionAsync(long chatId, FinanceDialog dialog, string? comment, CancellationToken ct)
    {
        var transactionId = await _sender.Send(
            new AddTransactionCommand(chatId, dialog.CategoryId!.Value, dialog.Amount!.Value, comment), ct);
        await ShowHomeAsync(chatId, notice: null, transactionId, ct);
    }

    private async Task SaveTemplateAsync(long chatId, FinanceDialog dialog, long? amount, CancellationToken ct)
    {
        await _sender.Send(new AddTemplateCommand(chatId, dialog.CategoryId!.Value, dialog.Title!, amount), ct);
        await ShowTemplatesAsync(chatId, dialog.Type, FinanceView.TemplateAddedNotice(dialog.Title!), ct);
    }

    // ---------- Helpers ----------

    private async Task StartDialogAsync(long chatId, FinanceDialog dialog, CancellationToken ct)
    {
        _dialogs.Set(chatId, dialog);
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task MoveToStepAsync(long chatId, FinanceStep from, FinanceStep to, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, from))
        {
            await ShowHomeAsync(chatId, ct);
            return;
        }

        dialog.Step = to;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task RenderDialogAsync(long chatId, FinanceDialog dialog, string? error, CancellationToken ct)
    {
        var categories = dialog.Step == FinanceStep.Category
            ? await _sender.Send(new GetCategoriesQuery(chatId, dialog.Type), ct)
            : null;

        await ShowAsync(chatId, FinanceView.Dialog(dialog, categories, error), ct);

        // The window may have been re-sent as a new message: listen to the current one
        dialog.ScreenId = _screens.Get(chatId);
    }

    /// <summary>A button of an old dialog step (or of no dialog at all) must not act on the current one.</summary>
    private bool TryGetDialog(long chatId, [NotNullWhen(true)] out FinanceDialog? dialog, params FinanceStep[] steps)
    {
        dialog = _dialogs.Get(chatId);
        return dialog is not null && steps.Contains(dialog.Step);
    }

    private async Task<TemplateDetailsDto> GetTemplateAsync(long chatId, Guid templateId, CancellationToken ct) =>
        await _sender.Send(new GetTemplateQuery(chatId, templateId), ct)
        ?? throw new NotFoundException(nameof(FinanceTemplate), templateId);

    private Task ShowAsync(long chatId, BotScreen screen, CancellationToken ct) =>
        _messenger.ShowScreenAsync(chatId, screen.Text, screen.Keyboard, ct);
}
