using System.Globalization;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Where a transaction card was opened from: "🔙" and the end of an action return there.
/// It travels in the buttons as a short token, so it survives a restart:
/// "m202610p2" is page 2 of October's operations, "d20261020" is a day of the calendar.
/// </summary>
public abstract record ReturnTo
{
    private const string MonthFormat = "yyyyMM";
    private const string DayFormat = "yyyyMMdd";

    private ReturnTo() { }

    /// <param name="Month">First day of the month.</param>
    /// <param name="Page">1-based.</param>
    public sealed record MonthPage(DateOnly Month, int Page) : ReturnTo;

    public sealed record Day(DateOnly Date) : ReturnTo;

    public string Token => this switch
    {
        MonthPage page => $"m{FormatMonth(page.Month)}p{page.Page}",
        Day day => $"d{FormatDay(day.Date)}",
        _ => throw new InvalidOperationException($"Unknown return target {GetType().Name}.")
    };

    public static ReturnTo? Parse(string? token) => token switch
    {
        ['d', .. var date] when TryParseDay(date, out var day) => new Day(day),
        ['m', .. var rest] when rest.Split('p') is [var month, var page]
            && TryParseMonth(month, out var first)
            && int.TryParse(page, NumberStyles.None, CultureInfo.InvariantCulture, out var number) => new MonthPage(first, number),
        _ => null
    };

    /// <summary>"202610": a month, read as its first day.</summary>
    public static bool TryParseMonth(string? text, out DateOnly month) => TryParseDate(text, MonthFormat, out month);

    /// <summary>"20261020": a day.</summary>
    public static bool TryParseDay(string? text, out DateOnly day) => TryParseDate(text, DayFormat, out day);

    public static string FormatMonth(DateOnly month) => month.ToString(MonthFormat, CultureInfo.InvariantCulture);
    public static string FormatDay(DateOnly day) => day.ToString(DayFormat, CultureInfo.InvariantCulture);

    private static bool TryParseDate(string? text, string format, out DateOnly date) =>
        DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
