namespace PersonalAssistant.Domain.Entities.Finance;

/// <summary>Calendar rules of scheduled payments.</summary>
public static class DueDates
{
    /// <summary>That day of the month, or its last day when the month is shorter (31 → 30 in April).</summary>
    public static DateOnly OnDay(int year, int month, int day)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 31);
        return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }

    /// <summary>Nearest date on that day of month, <paramref name="today"/> included.</summary>
    public static DateOnly NearestMonthDay(DateOnly today, int day)
    {
        var thisMonth = OnDay(today.Year, today.Month, day);
        if (thisMonth >= today)
            return thisMonth;

        var nextMonth = today.AddMonths(1);
        return OnDay(nextMonth.Year, nextMonth.Month, day);
    }

    /// <summary>Nearest date on that day of week, <paramref name="today"/> included.</summary>
    public static DateOnly NearestWeekday(DateOnly today, DayOfWeek day) =>
        today.AddDays(((int)day - (int)today.DayOfWeek + 7) % 7);
}
