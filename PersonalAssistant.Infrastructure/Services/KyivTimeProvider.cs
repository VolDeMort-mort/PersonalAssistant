namespace PersonalAssistant.Infrastructure.Services;

/// <summary>
/// System clock whose local time zone is Kyiv, whatever time zone the server runs in.
/// Month boundaries and reminders are calculated in this time zone.
/// </summary>
public sealed class KyivTimeProvider : TimeProvider
{
    private static readonly TimeZoneInfo Kyiv = FindKyiv();

    public override TimeZoneInfo LocalTimeZone => Kyiv;

    private static TimeZoneInfo FindKyiv()
    {
        // IANA ids work on Linux and on Windows with ICU; "FLE Standard Time" is the classic Windows id
        foreach (var id in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
        }

        throw new TimeZoneNotFoundException("Kyiv time zone is not available on this machine.");
    }
}
