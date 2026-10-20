using System.Globalization;
using System.Text;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// The finance calendar: a month as a grid of day buttons with marks, and a day with what is due on it
/// and what happened. Texts are HTML, so everything the user typed goes through <see cref="HtmlText.Encode"/>.
/// </summary>
public static class CalendarView
{
    /// <summary>How far ◀️ and ▶️ go from the current month, both ways.</summary>
    public const int MonthsAround = 12;

    private const int DaysInWeek = 7;

    // A button can't be empty: cells before the 1st and after the last day show this
    private const string EmptyCell = "·";

    public static BotScreen Month(PaymentCalendarDto calendar, DateOnly today)
    {
        var month = calendar.Month;
        const string body = "⚠️ прострочено · 💳 треба оплатити · ✅ оплачено\n📈 доходи · 📉 витрати\n\n"
            + "<i>Натисніть на день, щоб побачити деталі</i>";

        var rows = new List<InlineKeyboardButton[]>
        {
            PickerKeyboards.WeekFromMonday
                .Select(d => Button(ScreenText.Capitalize(DateText.Weekday(d)), FinancePayloads.Noop))
                .ToArray()
        };

        // The week starts on Monday, so Sunday's DayOfWeek 0 is the last column
        var emptyBefore = ((int)month.DayOfWeek + DaysInWeek - 1) % DaysInWeek;
        var cells = Enumerable.Range(0, emptyBefore).Select(_ => Button(EmptyCell, FinancePayloads.Noop))
            .Concat(calendar.Days.Select(d => Button(DayButton(d, today), FinancePayloads.Day(d.Date))))
            .ToList();
        while (cells.Count % DaysInWeek != 0)
            cells.Add(Button(EmptyCell, FinancePayloads.Noop));
        rows.AddRange(cells.Chunk(DaysInWeek));

        rows.Add(MonthNavigation(month, today));
        rows.Add(new[] { Button("📜 Операції місяця", FinancePayloads.History(month, page: 1)) });
        rows.Add(new[] { Button("🔙 Назад", FinancePayloads.Home) });

        var title = $"🗓 {DateText.MonthName(month.Month)} {month.Year}";
        return new BotScreen(ScreenText.Compose(title, body), new InlineKeyboardMarkup(rows));
    }

    /// <param name="recorded">A planned expense just paid from this day: the screen confirms it and offers undo.</param>
    public static BotScreen Day(FinanceDayDto day, DateOnly today, string? notice, TransactionDto? recorded)
    {
        var isPast = day.Date < today;
        var body = new StringBuilder();

        if (day.DuePayments.Count > 0)
        {
            body.Append(isPast ? "⚠️ <b>Прострочено</b>" : "💳 <b>До оплати</b>");
            foreach (var payment in day.DuePayments)
                body.Append('\n').Append($"{HtmlText.Encode(payment.Title)} · <b>{MoneyFormat.Amount(payment.Amount)}</b>");
            body.Append("\n\n");
        }

        var counted = day.Transactions.Count(t => !t.IsAdjustment);
        if (day.Transactions.Count == 0)
        {
            body.Append("📜 Операцій цього дня немає");
        }
        else
        {
            body.Append($"📜 <b>Операції</b> · {day.Transactions.Count}");
            if (counted > 0)
                body.Append($"\n📈 Доходи: <b>{MoneyFormat.Signed(day.Income)}</b>")
                    .Append($"\n📉 Витрати: <b>{MoneyFormat.Signed(-day.Expense)}</b>");
        }

        notice ??= recorded is null ? null : FinanceView.RecordedNotice(recorded);

        var rows = new List<InlineKeyboardButton[]>();
        if (recorded is not null)
            rows.Add(new[]
            {
                Button($"↩️ Скасувати: {FinanceView.Label(recorded)}", FinancePayloads.Undo(recorded.Id, new ReturnTo.Day(day.Date)))
            });

        rows.AddRange(KeyboardLayout.Pack(day.DuePayments.Select(p =>
            Button($"{(isPast ? "⚠️" : "💳")} {p.Title}", PaymentPayloads.Card(p.Id, day.Date)))));

        // A day reads in the order it happened; one per row, like the history
        rows.AddRange(day.Transactions
            .OrderBy(t => t.CreatedAtLocal)
            .Select(t => new[]
            {
                Button($"{t.CreatedAtLocal.ToString("HH:mm", CultureInfo.InvariantCulture)} · {FinanceView.Label(t)}",
                    FinancePayloads.Transaction(t.Id, new ReturnTo.Day(day.Date)))
            }));

        rows.Add(new[] { Button("🔙 До календаря", FinancePayloads.Calendar(day.Date)) });

        var title = $"🗓 {ScreenText.Capitalize(DateText.Short(day.Date))}{(day.Date == today ? " · сьогодні" : "")}";
        return new BotScreen(ScreenText.Compose(title, body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    /// <summary>
    /// The day's most important mark in place of its number: a number and an emoji don't fit into a seventh of a row,
    /// and the grid's position still tells the date. What still has to be paid comes before what is done,
    /// and income before expenses, which happen almost every day and would hide it.
    /// A day without a mark shows its number; today without a mark is in brackets.
    /// </summary>
    private static string DayButton(CalendarDayDto day, DateOnly today)
    {
        var mark = day switch
        {
            { HasOverdue: true } => "⚠️",
            { HasDue: true } => "💳",
            { HasPaid: true } => "✅",
            { HasIncome: true } => "📈",
            { HasExpenses: true } => "📉",
            _ => null
        };

        if (mark is not null)
            return mark;

        var number = day.Date.Day.ToString(CultureInfo.InvariantCulture);
        return day.Date == today ? $"[{number}]" : number;
    }

    /// <summary>"◀️ Вер", "Сьогодні" when another month is open, "Лис ▶️"; no further than <see cref="MonthsAround"/>.</summary>
    private static InlineKeyboardButton[] MonthNavigation(DateOnly month, DateOnly today)
    {
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var previous = month.AddMonths(-1);
        var next = month.AddMonths(1);

        var buttons = new List<InlineKeyboardButton>();
        if (previous >= thisMonth.AddMonths(-MonthsAround))
            buttons.Add(Button($"◀️ {DateText.MonthShortName(previous.Month)}", FinancePayloads.Calendar(previous)));
        if (month != thisMonth)
            buttons.Add(Button("Сьогодні", FinancePayloads.Calendar(thisMonth)));
        if (next <= thisMonth.AddMonths(MonthsAround))
            buttons.Add(Button($"{DateText.MonthShortName(next.Month)} ▶️", FinancePayloads.Calendar(next)));

        return buttons.ToArray();
    }

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
