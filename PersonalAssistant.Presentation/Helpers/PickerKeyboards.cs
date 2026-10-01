using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Helpers;

/// <summary>
/// Button grids for picking a date or a time, so nothing like "01.10" or "08:30" has to be typed.
/// Each feature passes its own payload builder.
/// </summary>
public static class PickerKeyboards
{
    public static readonly DayOfWeek[] WeekFromMonday =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    /// <summary>"Пн" … "Нд" in one row.</summary>
    public static IEnumerable<InlineKeyboardButton[]> Weekdays(Func<DayOfWeek, string> payload) =>
        KeyboardLayout.Grid(
            WeekFromMonday.Select(d => Button(ScreenText.Capitalize(DateText.Weekday(d)), payload(d))), perRow: 7);

    /// <summary>1 … 31, whatever the month.</summary>
    public static IEnumerable<InlineKeyboardButton[]> MonthDays(Func<int, string> payload) =>
        KeyboardLayout.Grid(Enumerable.Range(1, 31).Select(d => Button(d.ToString(), payload(d))), perRow: 7);

    /// <param name="count">How many months to offer, the current one first.</param>
    /// <param name="payload">Gets the first day of the month.</param>
    public static IEnumerable<InlineKeyboardButton[]> Months(DateOnly today, int count, Func<DateOnly, string> payload)
    {
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        return KeyboardLayout.Grid(
            Enumerable.Range(0, count)
                .Select(i => thisMonth.AddMonths(i))
                .Select(m => Button(MonthLabel(m, today), payload(m))), perRow: 3);
    }

    /// <summary>Days of <paramref name="month"/>; in the current month the past days are not offered.</summary>
    public static IEnumerable<InlineKeyboardButton[]> Days(DateOnly month, DateOnly today, Func<int, string> payload)
    {
        var firstDay = month.Year == today.Year && month.Month == today.Month ? today.Day : 1;
        var lastDay = DateTime.DaysInMonth(month.Year, month.Month);
        return KeyboardLayout.Grid(
            Enumerable.Range(firstDay, lastDay - firstDay + 1).Select(d => Button(d.ToString(), payload(d))), perRow: 7);
    }

    /// <summary>"07:00" … "22:00" for hours from <paramref name="first"/> to <paramref name="last"/>.</summary>
    public static IEnumerable<InlineKeyboardButton[]> Hours(int first, int last, Func<int, string> payload, int perRow = 4) =>
        KeyboardLayout.Grid(
            Enumerable.Range(first, last - first + 1).Select(h => Button($"{h:00}:00", payload(h))), perRow);

    private static string MonthLabel(DateOnly month, DateOnly today) =>
        month.Year == today.Year
            ? DateText.MonthShortName(month.Month)
            : $"{DateText.MonthShortName(month.Month)} {month.Year}";

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
