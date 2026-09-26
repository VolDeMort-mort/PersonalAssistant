using System.Globalization;
using System.Text;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Domain.Entities.Finance;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Screens of scheduled payments and the reminder message. Dates, days and times are picked
/// with buttons; only the title and amounts are typed.
/// </summary>
public static class PaymentView
{
    private static readonly int[] RemindDayOptions = { 0, 1, 2, 3, 7 };
    private const int FirstReminderHour = 7;
    private const int LastReminderHour = 22;
    private const int MonthsAhead = 12;

    private static readonly DayOfWeek[] WeekFromMonday =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public const string ChooseWithButtonsError = "⚠️ Оберіть варіант кнопкою";
    public const string DateInPastError = "⚠️ Ця дата вже минула";

    public static string AddedNotice(string title) => $"✅ Планову витрату «{HtmlText.Encode(title)}» додано";
    public static string DeletedNotice(string title) => $"🗑 Планову витрату «{HtmlText.Encode(title)}» видалено";

    public static BotScreen List(IReadOnlyList<ScheduledPaymentDto> payments, string? notice)
    {
        var body = payments.Count == 0
            ? "Планових витрат поки немає"
            : string.Join('\n', payments.Select(p => (p.IsOverdue ? "⚠️ " : "") + FinanceView.PaymentLine(p)));

        var rows = KeyboardLayout.Pack(payments.Select(p => Button(p.Title, PaymentPayloads.Card(p.Id)))).ToList();
        rows.Add(new[]
        {
            Button("➕ Додати", PaymentPayloads.New),
            Button("🔙 Назад", FinancePayloads.Home)
        });

        return new BotScreen(ScreenText.Compose("🗓 Планові витрати", body, notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen Card(ScheduledPaymentDto payment)
    {
        var body = string.Join('\n',
            $"Сума: <b>{MoneyFormat.Amount(payment.Amount)}</b>",
            $"Категорія: {HtmlText.Encode(payment.CategoryName)}",
            $"Наступна оплата: {(payment.IsOverdue ? "⚠️ " : "")}{DateText.Short(payment.NextDueDate)} · {DateText.DaysLeft(payment.DaysLeft)}",
            $"Періодичність: {RecurrenceLabel(payment.Recurrence)}",
            $"Нагадування: {ReminderLabel(payment.RemindDaysBefore, payment.RemindAt)}");

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Button("✅ Оплачено", PaymentPayloads.Paid(payment.Id)),
                Button("🗑 Видалити", PaymentPayloads.Delete(payment.Id))
            },
            new[] { Button("🔙 Назад", PaymentPayloads.List) }
        });

        return new BotScreen(ScreenText.Compose(payment.Title, body), keyboard);
    }

    /// <summary>"✅ Оплачено": the expected amount in one tap, another one is typed.</summary>
    public static BotScreen PayConfirm(ScheduledPaymentDto payment)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Button($"✅ {MoneyFormat.Amount(payment.Amount)}", PaymentPayloads.PayExpected(payment.Id)),
                Button("✏️ Інша сума", PaymentPayloads.PayOther(payment.Id))
            },
            new[] { Button("🔙 Назад", PaymentPayloads.Card(payment.Id)) }
        });

        return new BotScreen(ScreenText.Compose(payment.Title, "Скільки ви заплатили?"), keyboard);
    }

    /// <summary>A separate message, not the window: only a new message makes the phone notify.</summary>
    public static BotScreen Reminder(PaymentReminderDto reminder)
    {
        var body = $"{HtmlText.Encode(reminder.Title)} · <b>{MoneyFormat.Amount(reminder.Amount)}</b>\n"
            + $"{DateText.Short(reminder.DueDate)} · {DateText.DaysLeft(reminder.DaysLeft)}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Button("✅ Оплачено", PaymentPayloads.ReminderPaid(reminder.PaymentId)),
                Button("🙈 Закрити", PaymentPayloads.ReminderClose)
            }
        });

        return new BotScreen(ScreenText.Compose("🔔 Нагадування", body), keyboard);
    }

    /// <param name="categories">Needed only on the category step.</param>
    /// <param name="today">In the user's time zone: months and days in the past are not offered.</param>
    public static BotScreen Dialog(PaymentDialog dialog, IReadOnlyList<CategoryDto>? categories, DateOnly today, string? error)
    {
        var body = new StringBuilder();
        if (DraftSummary(dialog) is { } draft)
            body.Append(draft).Append("\n\n");
        body.Append(Prompt(dialog));

        var title = dialog.IsPaying ? dialog.Title ?? "" : "🗓 Нова планова витрата";
        return new BotScreen(ScreenText.Compose(title, body.ToString(), error), DialogKeyboard(dialog, categories, today));
    }

    public static string RecurrenceLabel(Recurrence recurrence) => recurrence switch
    {
        Recurrence.Weekly => "щотижня",
        Recurrence.Monthly => "щомісяця",
        Recurrence.Yearly => "щороку",
        _ => "разово"
    };

    public static string ReminderLabel(int? daysBefore, TimeOnly? at) =>
        daysBefore is { } days && at is { } time
            ? $"{RemindDaysLabel(days)} о {time.ToString("HH:mm", CultureInfo.InvariantCulture)}"
            : "без нагадування";

    private static string RemindDaysLabel(int days) => days switch
    {
        0 => "у день оплати",
        7 => "за тиждень",
        _ => $"за {DateText.Days(days)}"
    };

    /// <summary>What was entered so far, e.g. "«🏠 Оренда» · 8 000 ₴ · 🏠 Житло · щомісяця".</summary>
    private static string? DraftSummary(PaymentDialog dialog)
    {
        if (dialog.IsPaying)
            return null;

        var parts = new List<string>();
        if (dialog.Title is not null)
            parts.Add($"«{HtmlText.Encode(dialog.Title)}»");
        if (dialog.Amount is { } amount)
            parts.Add($"<b>{MoneyFormat.Amount(amount)}</b>");
        if (dialog.CategoryName is not null)
            parts.Add(HtmlText.Encode(dialog.CategoryName));
        if (dialog.Recurrence is { } recurrence)
            parts.Add(RecurrenceLabel(recurrence));
        if (dialog.FirstDueDate is { } date)
            parts.Add($"з {DateText.Short(date)}");
        if (dialog.Step == PaymentStep.RemindHour && dialog.RemindDaysBefore is { } days)
            parts.Add($"нагадати {RemindDaysLabel(days)}");

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string Prompt(PaymentDialog dialog) => dialog.Step switch
    {
        PaymentStep.Title => "Крок 1 з 7 · Надішліть назву, напр. <code>🏠 Оренда</code>",
        PaymentStep.Amount => "Крок 2 з 7 · Надішліть суму, напр. <code>8000</code>",
        PaymentStep.Category => "Крок 3 з 7 · Оберіть категорію або надішліть назву нової",
        PaymentStep.NewCategoryName => "Надішліть назву нової категорії. Емодзі на початку — за бажанням, напр. <code>🐶 Собака</code>",
        PaymentStep.Recurrence => "Крок 4 з 7 · Як часто платити?",
        PaymentStep.Weekday => "Крок 5 з 7 · В який день тижня?",
        PaymentStep.MonthDay => "Крок 5 з 7 · Якого числа? Якщо в місяці менше днів, оплата буде в останній день",
        PaymentStep.Month when dialog.Recurrence == Recurrence.Yearly => "Крок 5 з 7 · В якому місяці платити щороку?",
        PaymentStep.Month => "Крок 5 з 7 · В якому місяці оплата?",
        PaymentStep.Day when dialog.Month is { } month => $"Крок 5 з 7 · {DateText.MonthName(month.Month)} {month.Year}: якого числа?",
        PaymentStep.Day => "Крок 5 з 7 · Якого числа?",
        PaymentStep.RemindDays => "Крок 6 з 7 · Коли нагадати?",
        PaymentStep.RemindHour => "Крок 7 з 7 · О котрій нагадати?",
        _ => "Скільки ви заплатили? Надішліть суму, напр. <code>8000</code>"
    };

    private static InlineKeyboardMarkup DialogKeyboard(PaymentDialog dialog, IReadOnlyList<CategoryDto>? categories, DateOnly today)
    {
        var rows = new List<InlineKeyboardButton[]>();

        switch (dialog.Step)
        {
            case PaymentStep.Category:
                rows.AddRange(KeyboardLayout.Pack((categories ?? Array.Empty<CategoryDto>())
                    .Select(c => Button(c.Name, PaymentPayloads.Category(c.Id)))));
                break;

            case PaymentStep.Recurrence:
                rows.AddRange(KeyboardLayout.Grid(
                    new[] { Recurrence.Once, Recurrence.Weekly, Recurrence.Monthly, Recurrence.Yearly }
                        .Select(r => Button(Capitalize(RecurrenceLabel(r)), PaymentPayloads.Recurrence(r))), perRow: 2));
                break;

            case PaymentStep.Weekday:
                rows.AddRange(KeyboardLayout.Grid(
                    WeekFromMonday.Select(d => Button(Capitalize(DateText.Weekday(d)), PaymentPayloads.Weekday(d))), perRow: 7));
                break;

            case PaymentStep.MonthDay:
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(1, 31).Select(d => Button(d.ToString(), PaymentPayloads.MonthDay(d))), perRow: 7));
                break;

            case PaymentStep.Month:
                var thisMonth = new DateOnly(today.Year, today.Month, 1);
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(0, MonthsAhead)
                        .Select(i => thisMonth.AddMonths(i))
                        .Select(m => Button(MonthLabel(m, today), PaymentPayloads.Month(m))), perRow: 3));
                break;

            case PaymentStep.Day when dialog.Month is { } month:
                var firstDay = month.Year == today.Year && month.Month == today.Month ? today.Day : 1;
                var lastDay = DateTime.DaysInMonth(month.Year, month.Month);
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(firstDay, lastDay - firstDay + 1).Select(d => Button(d.ToString(), PaymentPayloads.Day(d))), perRow: 7));
                break;

            case PaymentStep.RemindDays:
                rows.AddRange(KeyboardLayout.Pack(
                    RemindDayOptions.Select(d => Button(Capitalize(RemindDaysLabel(d)), PaymentPayloads.RemindDays(d)))
                        .Prepend(Button("🔕 Без нагадування", PaymentPayloads.NoReminder))));
                break;

            case PaymentStep.RemindHour:
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(FirstReminderHour, LastReminderHour - FirstReminderHour + 1)
                        .Select(h => Button($"{h:00}:00", PaymentPayloads.RemindHour(h))), perRow: 4));
                break;
        }

        // The step's own action and the way out share the last row
        var lastRow = new List<InlineKeyboardButton>();
        if (dialog.Step == PaymentStep.Category)
            lastRow.Add(Button("➕ Нова категорія", PaymentPayloads.NewCategory));
        if (dialog.Step == PaymentStep.NewCategoryName)
            lastRow.Add(Button("🔙 До категорій", PaymentPayloads.BackToCategories));
        lastRow.Add(Button("✖️ Скасувати", PaymentPayloads.Cancel));
        rows.Add(lastRow.ToArray());

        return new InlineKeyboardMarkup(rows);
    }

    private static string MonthLabel(DateOnly month, DateOnly today) =>
        month.Year == today.Year
            ? DateText.MonthShortName(month.Month)
            : $"{DateText.MonthShortName(month.Month)} {month.Year}";

    private static string Capitalize(string text) => char.ToUpper(text[0]) + text[1..];

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
