namespace PersonalAssistant.Application.Common;

/// <param name="Month">First day of the month in the user's time zone.</param>
/// <param name="FromUtc">Start of the month in UTC, inclusive.</param>
/// <param name="ToUtc">Start of the next month in UTC, exclusive.</param>
public record MonthRange(DateOnly Month, DateTime FromUtc, DateTime ToUtc)
{
    public DateOnly LastDay => Month.AddMonths(1).AddDays(-1);
}

/// <param name="Date">The day in the user's time zone.</param>
/// <param name="FromUtc">Start of the day in UTC, inclusive.</param>
/// <param name="ToUtc">Start of the next day in UTC, exclusive.</param>
public record DayRange(DateOnly Date, DateTime FromUtc, DateTime ToUtc);

/// <summary>
/// The database stores UTC, the user lives in <see cref="TimeProvider.LocalTimeZone"/>:
/// these helpers translate between the two.
/// </summary>
public static class TimeProviderExtensions
{
    /// <summary>Calendar month that is current for the user, with bounds ready for database filters.</summary>
    public static MonthRange GetCurrentMonth(this TimeProvider time) => time.GetMonth(time.GetLocalToday());

    /// <summary>The calendar month <paramref name="anyDay"/> belongs to, with bounds ready for database filters.</summary>
    public static MonthRange GetMonth(this TimeProvider time, DateOnly anyDay)
    {
        var start = new DateTime(anyDay.Year, anyDay.Month, 1);

        return new MonthRange(DateOnly.FromDateTime(start), time.ToUtc(start), time.ToUtc(start.AddMonths(1)));
    }

    /// <summary>A day of the user, with bounds ready for database filters.</summary>
    public static DayRange GetDay(this TimeProvider time, DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        return new DayRange(date, time.ToUtc(start), time.ToUtc(start.AddDays(1)));
    }

    public static DateOnly GetLocalToday(this TimeProvider time) => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public static DateTime ToLocal(this TimeProvider time, DateTime utc) =>
        // EF reads datetime2 back without a Kind, but the database only holds UTC
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), time.LocalTimeZone);

    private static DateTime ToUtc(this TimeProvider time, DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(local, time.LocalTimeZone);
}
