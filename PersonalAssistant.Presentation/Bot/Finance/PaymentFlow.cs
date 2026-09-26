using System.Diagnostics.CodeAnalysis;
using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Features.Finance.Commands;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Features.Finance.Queries;
using PersonalAssistant.Domain.Entities.Finance;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Scheduled payment scenarios: the list, the card, paying, the new payment wizard
/// and the buttons of reminder messages.
/// </summary>
public class PaymentFlow
{
    private readonly ISender _sender;
    private readonly IBotMessenger _messenger;
    private readonly IDialogStore _dialogs;
    private readonly IScreenTracker _screens;
    private readonly TimeProvider _time;
    private readonly FinanceFlow _finance;

    public PaymentFlow(ISender sender, IBotMessenger messenger, IDialogStore dialogs, IScreenTracker screens,
        TimeProvider time, FinanceFlow finance)
    {
        _sender = sender;
        _messenger = messenger;
        _dialogs = dialogs;
        _screens = screens;
        _time = time;
        _finance = finance;
    }

    // ---------- Screens ----------

    public async Task ShowListAsync(long chatId, string? notice, CancellationToken ct)
    {
        _dialogs.Remove(chatId);

        var payments = await _sender.Send(new GetScheduledPaymentsQuery(chatId), ct);
        await ShowAsync(chatId, PaymentView.List(payments, notice), ct);
    }

    public async Task ShowCardAsync(long chatId, Guid paymentId, CancellationToken ct)
    {
        _dialogs.Remove(chatId);
        await ShowAsync(chatId, PaymentView.Card(await GetPaymentAsync(chatId, paymentId, ct)), ct);
    }

    // ---------- Paying ----------

    /// <summary>"✅ Оплачено": asks whether the expected amount was paid.</summary>
    public async Task AskPaidAmountAsync(long chatId, Guid paymentId, CancellationToken ct)
    {
        _dialogs.Remove(chatId);
        await ShowAsync(chatId, PaymentView.PayConfirm(await GetPaymentAsync(chatId, paymentId, ct)), ct);
    }

    /// <param name="amount">Null pays the expected amount.</param>
    public async Task PayAsync(long chatId, Guid paymentId, long? amount, CancellationToken ct)
    {
        var transactionId = await _sender.Send(new PayScheduledPaymentCommand(chatId, paymentId, amount), ct);

        // The dashboard confirms the expense and offers undo, like any other transaction
        await _finance.ShowHomeAsync(chatId, notice: null, transactionId, ct);
    }

    public async Task StartOtherAmountAsync(long chatId, Guid paymentId, CancellationToken ct) =>
        await StartDialogAsync(chatId, PaymentDialog.OtherAmount(await GetPaymentAsync(chatId, paymentId, ct)), ct);

    public async Task DeleteAsync(long chatId, Guid paymentId, CancellationToken ct)
    {
        var payment = await GetPaymentAsync(chatId, paymentId, ct);
        await _sender.Send(new DeleteScheduledPaymentCommand(chatId, paymentId), ct);
        await ShowListAsync(chatId, PaymentView.DeletedNotice(payment.Title), ct);
    }

    // ---------- Reminder message ----------

    public async Task PayFromReminderAsync(long chatId, int reminderMessageId, Guid paymentId, CancellationToken ct)
    {
        // The reminder goes first: after a restart it may have been taken as the window
        await _messenger.DeleteAsync(chatId, reminderMessageId, ct);
        await AskPaidAmountAsync(chatId, paymentId, ct);
    }

    public Task CloseReminderAsync(long chatId, int reminderMessageId, CancellationToken ct) =>
        _messenger.DeleteAsync(chatId, reminderMessageId, ct);

    // ---------- New payment wizard: buttons ----------

    public Task StartNewAsync(long chatId, CancellationToken ct) =>
        StartDialogAsync(chatId, PaymentDialog.NewPayment(), ct);

    public async Task ChooseCategoryAsync(long chatId, Guid categoryId, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.Category))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        var categories = await _sender.Send(new GetCategoriesQuery(chatId, TransactionType.Expense), ct);
        var category = categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new NotFoundException(nameof(FinanceCategory), categoryId);

        await OnCategoryChosenAsync(chatId, dialog, category, ct);
    }

    public Task AskNewCategoryAsync(long chatId, CancellationToken ct) =>
        MoveToStepAsync(chatId, from: PaymentStep.Category, to: PaymentStep.NewCategoryName, ct);

    public Task BackToCategoriesAsync(long chatId, CancellationToken ct) =>
        MoveToStepAsync(chatId, from: PaymentStep.NewCategoryName, to: PaymentStep.Category, ct);

    public async Task ChooseRecurrenceAsync(long chatId, Recurrence recurrence, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.Recurrence))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        dialog.Recurrence = recurrence;
        dialog.Step = recurrence switch
        {
            Recurrence.Weekly => PaymentStep.Weekday,
            Recurrence.Monthly => PaymentStep.MonthDay,
            _ => PaymentStep.Month
        };
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    public async Task ChooseWeekdayAsync(long chatId, int day, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.Weekday) || day is < 0 or > 6)
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        var date = DueDates.NearestWeekday(_time.GetLocalToday(), (DayOfWeek)day);
        await OnDateChosenAsync(chatId, dialog, date, anchorDay: null, ct);
    }

    public async Task ChooseMonthDayAsync(long chatId, int day, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.MonthDay) || day is < 1 or > 31)
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        // The 31st picked in September starts on the 30th but keeps returning to the 31st
        var date = DueDates.NearestMonthDay(_time.GetLocalToday(), day);
        await OnDateChosenAsync(chatId, dialog, date, anchorDay: day, ct);
    }

    /// <param name="yearMonth">E.g. 202610.</param>
    public async Task ChooseMonthAsync(long chatId, int yearMonth, CancellationToken ct)
    {
        var (year, month) = (yearMonth / 100, yearMonth % 100);
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.Month) || month is < 1 or > 12)
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        dialog.Month = new DateOnly(year, month, 1);
        dialog.Step = PaymentStep.Day;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    public async Task ChooseDayAsync(long chatId, int day, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.Day) || dialog.Month is not { } month
            || day < 1 || day > DateTime.DaysInMonth(month.Year, month.Month))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        var date = new DateOnly(month.Year, month.Month, day);
        if (date < _time.GetLocalToday())
        {
            await RenderDialogAsync(chatId, dialog, PaymentView.DateInPastError, ct);
            return;
        }

        await OnDateChosenAsync(chatId, dialog, date, anchorDay: day, ct);
    }

    /// <param name="daysBefore">Null: no reminder, the payment is saved right away.</param>
    public async Task ChooseRemindDaysAsync(long chatId, int? daysBefore, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.RemindDays)
            || daysBefore is < 0 or > ScheduledPayment.MaxRemindDaysBefore)
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        if (daysBefore is null)
        {
            await SavePaymentAsync(chatId, dialog, remindAt: null, ct);
            return;
        }

        dialog.RemindDaysBefore = daysBefore;
        dialog.Step = PaymentStep.RemindHour;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    public async Task ChooseRemindHourAsync(long chatId, int hour, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, PaymentStep.RemindHour) || hour is < 0 or > 23)
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        await SavePaymentAsync(chatId, dialog, new TimeOnly(hour, 0), ct);
    }

    public async Task CancelAsync(long chatId, CancellationToken ct)
    {
        if (_dialogs.Get<PaymentDialog>(chatId)?.PaymentId is { } paymentId)
            await ShowCardAsync(chatId, paymentId, ct);
        else
            await ShowListAsync(chatId, notice: null, ct);
    }

    // ---------- Typed text ----------

    public bool IsWaitingForText(long chatId) =>
        _dialogs.Get<PaymentDialog>(chatId) is { } dialog && dialog.ScreenId == _screens.Get(chatId);

    public async Task HandleTextAsync(long chatId, string? text, CancellationToken ct)
    {
        var dialog = _dialogs.Get<PaymentDialog>(chatId);
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
            case PaymentStep.Title:
                await OnTitleAsync(chatId, dialog, text, ct);
                break;
            case PaymentStep.Amount:
                await OnAmountAsync(chatId, dialog, text, ct);
                break;
            // Typing on the category step is a shortcut for "➕ Нова категорія"
            case PaymentStep.Category:
            case PaymentStep.NewCategoryName:
                await OnNewCategoryAsync(chatId, dialog, text, ct);
                break;
            case PaymentStep.PaidAmount:
                await OnPaidAmountAsync(chatId, dialog, text, ct);
                break;
            default:
                await RenderDialogAsync(chatId, dialog, PaymentView.ChooseWithButtonsError, ct);
                break;
        }
    }

    private async Task OnTitleAsync(long chatId, PaymentDialog dialog, string text, CancellationToken ct)
    {
        if (text.Length > ScheduledPayment.MaxTitleLength)
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(ScheduledPayment.MaxTitleLength), ct);
            return;
        }

        dialog.Title = text;
        dialog.Step = PaymentStep.Amount;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnAmountAsync(long chatId, PaymentDialog dialog, string text, CancellationToken ct)
    {
        if (!MoneyFormat.TryParse(text, allowZeroOrNegative: false, out var amount))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.AmountError, ct);
            return;
        }

        dialog.Amount = amount;
        dialog.Step = PaymentStep.Category;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnNewCategoryAsync(long chatId, PaymentDialog dialog, string text, CancellationToken ct)
    {
        if (text.Length > FinanceCategory.MaxNameLength)
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.TooLongError(FinanceCategory.MaxNameLength), ct);
            return;
        }

        var categoryId = await _sender.Send(new AddCategoryCommand(chatId, TransactionType.Expense, text), ct);

        // The command may return an existing category written differently, e.g. "житло" for "🏠 Житло"
        var categories = await _sender.Send(new GetCategoriesQuery(chatId, TransactionType.Expense), ct);
        await OnCategoryChosenAsync(chatId, dialog, categories.First(c => c.Id == categoryId), ct);
    }

    private async Task OnPaidAmountAsync(long chatId, PaymentDialog dialog, string text, CancellationToken ct)
    {
        if (!MoneyFormat.TryParse(text, allowZeroOrNegative: false, out var amount))
        {
            await RenderDialogAsync(chatId, dialog, FinanceView.AmountError, ct);
            return;
        }

        await PayAsync(chatId, dialog.PaymentId!.Value, amount, ct);
    }

    private async Task OnCategoryChosenAsync(long chatId, PaymentDialog dialog, CategoryDto category, CancellationToken ct)
    {
        dialog.CategoryId = category.Id;
        dialog.CategoryName = category.Name;
        dialog.Step = PaymentStep.Recurrence;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task OnDateChosenAsync(long chatId, PaymentDialog dialog, DateOnly date, int? anchorDay, CancellationToken ct)
    {
        dialog.FirstDueDate = date;
        dialog.AnchorDay = anchorDay;
        dialog.Step = PaymentStep.RemindDays;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task SavePaymentAsync(long chatId, PaymentDialog dialog, TimeOnly? remindAt, CancellationToken ct)
    {
        await _sender.Send(new AddScheduledPaymentCommand(
            chatId,
            dialog.CategoryId!.Value,
            dialog.Title!,
            dialog.Amount!.Value,
            dialog.Recurrence!.Value,
            dialog.FirstDueDate!.Value,
            remindAt is null ? null : dialog.RemindDaysBefore,
            remindAt,
            dialog.AnchorDay), ct);

        await ShowListAsync(chatId, PaymentView.AddedNotice(dialog.Title!), ct);
    }

    // ---------- Helpers ----------

    private async Task StartDialogAsync(long chatId, PaymentDialog dialog, CancellationToken ct)
    {
        _dialogs.Set(chatId, dialog);
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task MoveToStepAsync(long chatId, PaymentStep from, PaymentStep to, CancellationToken ct)
    {
        if (!TryGetDialog(chatId, out var dialog, from))
        {
            await ShowListAsync(chatId, notice: null, ct);
            return;
        }

        dialog.Step = to;
        await RenderDialogAsync(chatId, dialog, error: null, ct);
    }

    private async Task RenderDialogAsync(long chatId, PaymentDialog dialog, string? error, CancellationToken ct)
    {
        var categories = dialog.Step == PaymentStep.Category
            ? await _sender.Send(new GetCategoriesQuery(chatId, TransactionType.Expense), ct)
            : null;

        await ShowAsync(chatId, PaymentView.Dialog(dialog, categories, _time.GetLocalToday(), error), ct);

        // The window may have been re-sent as a new message: listen to the current one
        dialog.ScreenId = _screens.Get(chatId);
    }

    /// <summary>A button of an old step (or of no dialog at all) must not act on the current one.</summary>
    private bool TryGetDialog(long chatId, [NotNullWhen(true)] out PaymentDialog? dialog, PaymentStep step)
    {
        dialog = _dialogs.Get<PaymentDialog>(chatId);
        return dialog is not null && dialog.Step == step;
    }

    private async Task<ScheduledPaymentDto> GetPaymentAsync(long chatId, Guid paymentId, CancellationToken ct) =>
        await _sender.Send(new GetScheduledPaymentQuery(chatId, paymentId), ct)
        ?? throw new NotFoundException(nameof(ScheduledPayment), paymentId);

    private Task ShowAsync(long chatId, BotScreen screen, CancellationToken ct) =>
        _messenger.ShowScreenAsync(chatId, screen.Text, screen.Keyboard, ct);
}
