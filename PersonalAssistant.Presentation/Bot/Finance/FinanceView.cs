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
    private static readonly string[] MonthNames =
    {
        "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
        "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
    };

    // Under every title; as the widest line it also keeps all finance windows the same width
    private const string Separator = "────────────────────────";

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
        var body = string.Join('\n',
            $"💳 Баланс: <b>{MoneyFormat.Amount(dashboard.Balance)}</b>",
            "",
            $"📈 Доходи: <b>{MoneyFormat.Signed(dashboard.MonthIncome)}</b>",
            $"📉 Витрати: <b>{MoneyFormat.Signed(-dashboard.MonthExpense)}</b>",
            Separator,
            $"{MonthResultLabel(dashboard.MonthResult)}: <b>{MoneyFormat.Signed(dashboard.MonthResult)}</b>");

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
            Button("⚖️ Коригування", FinancePayloads.Balance),
            Button("🔙 Назад", BotConstants.Payloads.NavRoot)
        });

        var title = $"💰 Фінанси · {MonthNames[dashboard.Month.Month - 1]}";
        return new BotScreen(Compose(title, body, notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen Templates(TransactionType type, IReadOnlyList<TemplateDto> templates, string? notice)
    {
        var body = templates.Count == 0
            ? "Шаблонів поки немає: додайте перший або введіть вручну"
            : "Оберіть шаблон або введіть вручну";

        var rows = KeyboardLayout
            .Pack(templates.Select(t => Button(WithAmount(t.Title, t.Amount), FinancePayloads.Template(t.Id))))
            .ToList();
        rows.Add(new[]
        {
            Button("✍️ Вручну", FinancePayloads.Manual(type)),
            Button("➕ Новий шаблон", FinancePayloads.NewTemplate(type))
        });
        rows.Add(new[] { Button("🔙 Назад", FinancePayloads.Home) });

        var title = type == TransactionType.Expense ? "➖ Оплатити" : "➕ Зарахувати";
        return new BotScreen(Compose(title, body, notice), new InlineKeyboardMarkup(rows));
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

        return new BotScreen(Compose(template.Title, body, notice: null), keyboard);
    }

    /// <param name="categories">Needed only on the category step.</param>
    public static BotScreen Dialog(FinanceDialog dialog, IReadOnlyList<CategoryDto>? categories, string? error)
    {
        var body = new StringBuilder();
        if (DraftSummary(dialog) is { } draft)
            body.Append(draft).Append("\n\n");
        body.Append(Prompt(dialog));

        return new BotScreen(Compose(DialogTitle(dialog), body.ToString(), error), DialogKeyboard(dialog, categories));
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

    /// <summary>Centered title, separator, body and an optional notice at the bottom.</summary>
    /// <param name="title">Plain text: it is encoded here, after its width is measured.</param>
    private static string Compose(string title, string body, string? notice)
    {
        var padding = TextLayout.CenterPadding(title, Separator, bold: true);
        var text = $"{padding}<b>{HtmlText.Encode(title)}</b>\n{Separator}\n{body}";
        return notice is null ? text : $"{text}\n\n{notice}";
    }

    private static string MonthResultLabel(long result) => result switch
    {
        > 0 => "🟢 Місяць у плюсі",
        < 0 => "🔴 Місяць у мінусі",
        _ => "⚪ Місяць"
    };

    /// <summary>Plain text, e.g. "🚌 Проїзд −30 ₴": used in buttons and (encoded) in notices.</summary>
    private static string Label(TransactionDto transaction)
    {
        var what = transaction.IsAdjustment
            ? "⚖️ Коригування"
            : transaction.Comment ?? transaction.CategoryName ?? "Операція";
        return $"{what} {MoneyFormat.Signed(transaction.Amount, transaction.Type)}";
    }

    private static string WithAmount(string text, long? amount) =>
        amount is { } value ? $"{text} · {MoneyFormat.Amount(value)}" : text;

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
