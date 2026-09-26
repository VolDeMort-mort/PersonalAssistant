using System.Globalization;

namespace PersonalAssistant.Presentation.Helpers;

/// <summary>Dates and day counts in Ukrainian: "чт, 01.10", "через 5 днів".</summary>
public static class DateText
{
    private static readonly string[] Weekdays = { "нд", "пн", "вт", "ср", "чт", "пт", "сб" };

    private static readonly string[] MonthNames =
    {
        "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
        "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
    };

    private static readonly string[] MonthShortNames =
    {
        "Січ", "Лют", "Бер", "Кві", "Тра", "Чер", "Лип", "Сер", "Вер", "Жов", "Лис", "Гру"
    };

    public static string MonthName(int month) => MonthNames[month - 1];
    public static string MonthShortName(int month) => MonthShortNames[month - 1];

    /// <summary>"пн", "вт"… in <see cref="DayOfWeek"/> order.</summary>
    public static string Weekday(DayOfWeek day) => Weekdays[(int)day];

    public static string Short(DateOnly date) =>
        $"{Weekday(date.DayOfWeek)}, {date.ToString("dd.MM", CultureInfo.InvariantCulture)}";

    /// <param name="days">0 today, negative when overdue.</param>
    public static string DaysLeft(int days) => days switch
    {
        0 => "сьогодні",
        1 => "завтра",
        > 1 => $"через {Days(days)}",
        _ => $"прострочено на {Days(-days)}"
    };

    public static string Days(int count) => $"{count} {Plural(count, "день", "дні", "днів")}";

    /// <summary>Ukrainian plural: 1 день, 2 дні, 5 днів, 11 днів, 21 день.</summary>
    public static string Plural(int count, string one, string few, string many)
    {
        var lastTwo = Math.Abs(count) % 100;
        if (lastTwo is >= 11 and <= 14)
            return many;

        return (lastTwo % 10) switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many
        };
    }
}
