using System.Globalization;
using System.Text;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;
using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Turns finance data into screens. The only place that knows how finances look:
/// texts are HTML, so everything the user typed goes through <see cref="HtmlText.Encode"/>.
/// </summary>
public static class FinanceView
{
    // Overdue payments listed on the dashboard; the rest are behind "➖ Оплатити" → "🗓 Планові витрати"
    private const int MaxOverdueShown = 3;

    public const string GoneNotice = "⚠️ Цього запису вже не існує";
    public const string AlreadyUndoneNotice = "ℹ️ Операцію вже скасовано";
    public const string BalanceUnchangedNotice = "ℹ️ Баланс і так збігається";

    public const string AmountError = "⚠️ Не схоже на суму. Надішліть ціле число без копійок, напр. <code>250</code>";
    public const string BalanceError = "⚠️ Не схоже на суму. Надішліть ціле число без копійок, можна з мінусом, напр. <code>-500</code>";
    public const string NotTextError = "⚠️ Надішліть відповідь текстом";

    public static string TooLongError(int maxLength) => $"⚠️ Задовго: максимум {maxLength} символів";
    public static string RecordedNotice(TransactionDto transaction) => $"✅ Записано: {HtmlText.Encode(Label(transaction))}";
    public static string UndoneNotice(TransactionDto transaction) => $"↩️ Скасовано: {HtmlText.Encode(Label(transaction))}";
    public static string TemplateAddedNotice(string title) => $"✅ Шаблон «{HtmlText.Encode(title)}» додано";
    public static string TemplateDeletedNotice(string title) => $"🗑 Шаблон «{HtmlText.Encode(title)}» видалено";

    /// <param name="recorded">Just recorded transaction: confirmed in the text and offered for undo.</param>
    public static BotScreen Dashboard(FinanceDashboardDto dashboard, string? notice, TransactionDto? recorded)
    {
        var body = new StringBuilder(string.Join('\n',
            $"💳 Баланс: <b>{MoneyFormat.Amount(dashboard.Balance)}</b>",
            "",
            $"📈 Доходи: <b>{MoneyFormat.Signed(dashboard.MonthIncome)}</b>",
            $"📉 Витрати: <b>{MoneyFormat.Signed(-dashboard.MonthExpense)}</b>",
            ScreenText.Separator,
            $"{MonthResultLabel(dashboard.MonthResult)}: <b>{MoneyFormat.Signed(dashboard.MonthResult)}</b>"));

        if (dashboard.OverduePayments.Count > 0)
        {
            body.Append("\n\n⚠️ <b>Прострочено</b>");
            foreach (var payment in dashboard.OverduePayments.Take(MaxOverdueShown))
                body.Append('\n').Append(PaymentLine(payment));
            if (dashboard.OverduePayments.Count > MaxOverdueShown)
                body.Append($"\nі ще {dashboard.OverduePayments.Count - MaxOverdueShown}");
        }

        if (dashboard.NextPayment is { } next)
        {
            body.Append("\n\n⏰ <b>Найближча оплата</b>\n")
                .Append($"{HtmlText.Encode(next.Title)} · <b>{MoneyFormat.Amount(next.Amount)}</b>\n")
                .Append($"{DateText.Short(next.NextDueDate)} · {DateText.DaysLeft(next.DaysLeft)}");
        }

        notice ??= recorded is null ? null : RecordedNotice(recorded);

        var rows = new List<InlineKeyboardButton[]>();
        if (recorded is not null)
            rows.Add(new[] { Button($"↩️ Скасувати: {Label(recorded)}", FinancePayloads.Undo(recorded.Id)) });
        rows.Add(new[]
        {
            Button("➕ Зарахувати", FinancePayloads.Templates(TransactionType.Income)),
            Button("➖ Оплатити", FinancePayloads.Templates(TransactionType.Expense))
        });
        rows.Add(new[]
        {
            Button("📜 Історія", FinancePayloads.History(page: 1)),
            Button("⚖️ Коригування", FinancePayloads.Balance)
        });
        rows.Add(new[] { Button("🔙 Назад", BotConstants.Payloads.NavRoot) });

        var title = $"💰 Фінанси · {DateText.MonthName(dashboard.Month.Month)}";
        return new BotScreen(ScreenText.Compose(title, body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen History(HistoryPageDto history, string? notice)
    {
        var body = new StringBuilder();
        if (history.TopExpenses.Count > 0)
        {
            body.Append("🔥 <b>Топ витрат</b>");
            foreach (var category in history.TopExpenses)
            {
                body.Append('\n').Append($"{HtmlText.Encode(category.CategoryName)} · <b>{MoneyFormat.Amount(category.Total)}</b>");
                if (history.MonthExpense > 0)
                    body.Append($" · {Math.Round(category.Total * 100.0 / history.MonthExpense)}%");
            }
            body.Append('\n').Append(ScreenText.Separator).Append('\n');
        }

        body.Append(history.TotalCount == 0
            ? "Цього місяця операцій ще немає"
            : $"{history.TotalCount} {DateText.Plural(history.TotalCount, "операція", "операції", "операцій")} · сторінка {history.Page} з {history.PageCount}");

        // A chronological list: one per row even when short, so the order is obvious
        var rows = history.Transactions
            .Select(t => new[] { Button($"{t.CreatedAtLocal.ToString("dd.MM", CultureInfo.InvariantCulture)} · {Label(t)}",
                FinancePayloads.Transaction(t.Id, history.Page)) })
            .ToList();

        if (history.PageCount > 1)
        {
            var pages = new List<InlineKeyboardButton>();
            if (history.Page > 1)
                pages.Add(Button("◀️", FinancePayloads.History(history.Page - 1)));
            pages.Add(Button($"{history.Page} / {history.PageCount}", FinancePayloads.History(history.Page)));
            if (history.Page < history.PageCount)
                pages.Add(Button("▶️", FinancePayloads.History(history.Page + 1)));
            rows.Add(pages.ToArray());
        }
        rows.Add(new[] { Button("🔙 Назад", FinancePayloads.Home) });

        var title = $"📜 Історія · {DateText.MonthName(history.Month.Month)}";
        return new BotScreen(ScreenText.Compose(title, body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    /// <param name="page">History page to return to.</param>
    /// <param name="confirmDelete">Deleting can't be undone, so it is asked once more.</param>
    public static BotScreen TransactionCard(TransactionDto transaction, int page, bool confirmDelete)
    {
        var lines = new List<string> { $"Сума: <b>{MoneyFormat.Signed(transaction.Amount, transaction.Type)}</b>" };
        if (transaction.Comment is not null && transaction.CategoryName is not null)
            lines.Add($"Категорія: {HtmlText.Encode(transaction.CategoryName)}");
        lines.Add($"Дата: {DateText.Short(DateOnly.FromDateTime(transaction.CreatedAtLocal))} · "
            + transaction.CreatedAtLocal.ToString("HH:mm", CultureInfo.InvariantCulture));
        if (transaction.IsScheduledPayment)
            lines.Add("🗓 Оплата планової витрати");
        if (transaction.IsAdjustment)
            lines.Add("Не входить у підсумки місяця");

        string? question = null;
        if (confirmDelete)
        {
            // Deleting an expense gives the money back to the balance, deleting income takes it away
            var balanceChange = transaction.Type == TransactionType.Expense ? transaction.Amount : -transaction.Amount;
            question = $"🗑 Видалити цю операцію? Баланс зміниться на <b>{MoneyFormat.Signed(balanceChange)}</b>";
            if (transaction.IsScheduledPayment)
                question += "\nЯкщо це остання оплата планової витрати, її дата повернеться назад";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            confirmDelete
                ? new[]
                {
                    Button("🗑 Так, видалити", FinancePayloads.ConfirmDeleteTransaction(transaction.Id, page)),
                    Button("✖️ Ні", FinancePayloads.Transaction(transaction.Id, page))
                }
                : new[]
                {
                    Button("🗑 Видалити", FinancePayloads.DeleteTransaction(transaction.Id, page)),
                    Button("🔙 Назад", FinancePayloads.History(page))
                }
        });

        return new BotScreen(ScreenText.Compose(What(transaction), string.Join('\n', lines), question), keyboard);
    }

    public static string DeletedNotice(TransactionDto transaction) => $"🗑 Видалено: {HtmlText.Encode(Label(transaction))}";

    /// <summary>"📱 Мобільний · 250 ₴ · пн, 29.09".</summary>
    public static string PaymentLine(ScheduledPaymentDto payment) =>
        $"{HtmlText.Encode(payment.Title)} · <b>{MoneyFormat.Amount(payment.Amount)}</b> · {DateText.Short(payment.NextDueDate)}";

    public static BotScreen Templates(TransactionType type, IReadOnlyList<TemplateDto> templates, string? notice)
    {
        var body = templates.Count == 0
            ? "Шаблонів поки немає: додайте перший або введіть вручну"
            : "Оберіть шаблон або введіть вручну";

        var rows = KeyboardLayout
            .Pack(templates.Select(t => Button(WithAmount(t.Title, t.Amount), FinancePayloads.Template(t.Id))))
            .ToList();
        // Planned expenses are paid from here too, so they live on the expense screen
        if (type == TransactionType.Expense)
            rows.Add(new[] { Button("🗓 Планові витрати", PaymentPayloads.List) });
        rows.Add(new[]
        {
            Button("✍️ Вручну", FinancePayloads.Manual(type)),
            Button("➕ Новий шаблон", FinancePayloads.NewTemplate(type))
        });
        rows.Add(new[] { Button("🔙 Назад", FinancePayloads.Home) });

        var title = type == TransactionType.Expense ? "➖ Оплатити" : "➕ Зарахувати";
        return new BotScreen(ScreenText.Compose(title, body, notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen TemplateCard(TemplateDetailsDto template)
    {
        var amount = template.Amount is { } value ? $"<b>{MoneyFormat.Amount(value)}</b>" : "вводиться щоразу";
        var body = $"Категорія: {HtmlText.Encode(template.CategoryName)}\nСума: {amount}";
        // No amount on the button: it is in the text above and would not fit into half a row
        var action = template.Type == TransactionType.Expense ? "✅ Оплатити" : "✅ Зарахувати";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Button(action, FinancePayloads.UseTemplate(template.Id)),
                Button("🗑 Видалити", FinancePayloads.DeleteTemplate(template.Id))
            },
            new[] { Button("🔙 Назад", FinancePayloads.Templates(template.Type)) }
        });

        return new BotScreen(ScreenText.Compose(template.Title, body, notice: null), keyboard);
    }

    /// <param name="categories">Needed only on the category step.</param>
    public static BotScreen Dialog(FinanceDialog dialog, IReadOnlyList<CategoryDto>? categories, string? error)
    {
        var body = new StringBuilder();
        if (DraftSummary(dialog) is { } draft)
            body.Append(draft).Append("\n\n");
        body.Append(Prompt(dialog));

        return new BotScreen(ScreenText.Compose(DialogTitle(dialog), body.ToString(), error), DialogKeyboard(dialog, categories));
    }

    private static string DialogTitle(FinanceDialog dialog) => dialog.Kind switch
    {
        FinanceDialogKind.ManualEntry => dialog.Type == TransactionType.Expense ? "➖ Оплата вручну" : "➕ Зарахування вручну",
        FinanceDialogKind.NewTemplate => dialog.Type == TransactionType.Expense ? "🧩 Новий шаблон витрати" : "🧩 Новий шаблон доходу",
        FinanceDialogKind.TemplateUse => dialog.Title ?? "",
        _ => dialog.IsFirstVisit ? "💰 Фінанси" : "⚖️ Коригування балансу"
    };

    /// <summary>What was entered so far, e.g. "250 ₴ · 🍔 Їжа".</summary>
    private static string? DraftSummary(FinanceDialog dialog)
    {
        if (dialog.Kind == FinanceDialogKind.Balance)
            return dialog.IsFirstVisit ? null : $"Зараз у боті: <b>{MoneyFormat.Amount(dialog.KnownBalance ?? 0)}</b>";

        var parts = new List<string>();
        if (dialog.Kind == FinanceDialogKind.NewTemplate && dialog.Title is not null)
            parts.Add($"«{HtmlText.Encode(dialog.Title)}»");
        if (dialog.Amount is { } amount)
            parts.Add($"<b>{MoneyFormat.Amount(amount)}</b>");
        if (dialog.CategoryName is not null)
            parts.Add(HtmlText.Encode(dialog.CategoryName));

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string Prompt(FinanceDialog dialog) => dialog.Step switch
    {
        FinanceStep.Amount when dialog.Kind == FinanceDialogKind.TemplateUse => "Надішліть суму, напр. <code>250</code>",
        FinanceStep.Amount => "Крок 1 з 3 · Надішліть суму, напр. <code>250</code>",
        FinanceStep.Title => "Крок 1 з 3 · Надішліть назву шаблону, напр. <code>☕ Кава</code>",
        FinanceStep.Category => "Крок 2 з 3 · Оберіть категорію або надішліть назву нової",
        FinanceStep.NewCategoryName => "Надішліть назву нової категорії. Емодзі на початку — за бажанням, напр. <code>🐶 Собака</code>",
        FinanceStep.Comment when dialog.Type == TransactionType.Expense => "Крок 3 з 3 · На що саме? Надішліть коментар або пропустіть",
        FinanceStep.Comment => "Крок 3 з 3 · Звідки гроші? Надішліть коментар або пропустіть",
        FinanceStep.TemplateAmount => "Крок 3 з 3 · Надішліть суму шаблону або оберіть «Питати щоразу»",
        _ when dialog.IsFirstVisit => "Вітаю! Скільки грошей у вас зараз?\nНадішліть суму одним числом, напр. <code>12500</code>",
        _ => "Надішліть, скільки грошей у вас насправді.\nРізниця запишеться як коригування і не вплине на підсумки місяця."
    };

    private static InlineKeyboardMarkup DialogKeyboard(FinanceDialog dialog, IReadOnlyList<CategoryDto>? categories)
    {
        var rows = new List<InlineKeyboardButton[]>();
        if (dialog.Step == FinanceStep.Category)
            rows.AddRange(KeyboardLayout.Pack((categories ?? Array.Empty<CategoryDto>())
                .Select(c => Button(c.Name, FinancePayloads.Category(c.Id)))));

        // The step's own action and the way out share the last row
        var lastRow = new List<InlineKeyboardButton>();
        if (StepAction(dialog.Step) is { } action)
            lastRow.Add(action);
        lastRow.Add(dialog.IsFirstVisit
            ? Button("Почати з 0 ₴", FinancePayloads.Home)
            : Button("✖️ Скасувати", FinancePayloads.Cancel));
        rows.Add(lastRow.ToArray());

        return new InlineKeyboardMarkup(rows);
    }

    private static InlineKeyboardButton? StepAction(FinanceStep step) => step switch
    {
        FinanceStep.Category => Button("➕ Нова категорія", FinancePayloads.NewCategory),
        FinanceStep.NewCategoryName => Button("🔙 До категорій", FinancePayloads.BackToCategories),
        FinanceStep.Comment => Button("⏭ Пропустити", FinancePayloads.Skip),
        FinanceStep.TemplateAmount => Button("🔢 Питати щоразу", FinancePayloads.Skip),
        _ => null
    };

    private static string MonthResultLabel(long result) => result switch
    {
        > 0 => "🟢 Місяць у плюсі",
        < 0 => "🔴 Місяць у мінусі",
        _ => "⚪ Місяць"
    };

    /// <summary>Plain text, e.g. "🚌 Проїзд −30 ₴": used in buttons and (encoded) in notices.</summary>
    private static string Label(TransactionDto transaction) =>
        $"{What(transaction)} {MoneyFormat.Signed(transaction.Amount, transaction.Type)}";

    /// <summary>What the transaction was: its comment, else its category.</summary>
    private static string What(TransactionDto transaction) =>
        transaction.IsAdjustment
            ? "⚖️ Коригування"
            : transaction.Comment ?? transaction.CategoryName ?? "Операція";

    private static string WithAmount(string text, long? amount) =>
        amount is { } value ? $"{text} · {MoneyFormat.Amount(value)}" : text;

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
