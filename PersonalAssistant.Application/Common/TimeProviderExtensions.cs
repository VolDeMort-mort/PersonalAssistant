namespace PersonalAssistant.Application.Common;

/// <param name="Month">First day of the month in the user's time zone.</param>
/// <param name="FromUtc">Start of the month in UTC, inclusive.</param>
/// <param name="ToUtc">Start of the next month in UTC, exclusive.</param>
public record MonthRange(DateOnly Month, DateTime FromUtc, DateTime ToUtc);

/// <summary>
/// The database stores UTC, the user lives in <see cref="TimeProvider.LocalTimeZone"/>:
/// these helpers translate between the two.
/// </summary>
public static class TimeProviderExtensions
{
    /// <summary>Calendar month that is current for the user, with bounds ready for database filters.</summary>
    public static MonthRange GetCurrentMonth(this TimeProvider time)
    {
        var localNow = time.GetLocalNow();
        var start = new DateTime(localNow.Year, localNow.Month, 1);

        return new MonthRange(
            DateOnly.FromDateTime(start),
            TimeZoneInfo.ConvertTimeToUtc(start, time.LocalTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(start.AddMonths(1), time.LocalTimeZone));
    }

    public static DateTime ToLocal(this TimeProvider time, DateTime utc) =>
        // EF reads datetime2 back without a Kind, but the database only holds UTC
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), time.LocalTimeZone);
}
