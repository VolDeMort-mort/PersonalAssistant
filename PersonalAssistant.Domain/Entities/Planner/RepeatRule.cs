using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Planner;

/// <summary>
/// How a task repeats: a value object. It is immutable and compared by value,
/// so changing the repeat of a task means giving it another rule.
/// All calendar math of repeats lives here: nobody else adds days or months by hand.
/// </summary>
public sealed record RepeatRule
{
    public const int MinInterval = 2;
    public const int MaxInterval = 30;

    // For EF: the rule is stored in the columns of its task
    private RepeatRule() { }

    public RepeatKind Kind { get; private init; }

    /// <summary>Days between occurrences; 1 for <see cref="RepeatKind.Daily"/>, 0 when not used.</summary>
    public int Interval { get; private init; }

    /// <summary>Weekdays of <see cref="RepeatKind.Weekly"/>; <see cref="WeekDays.None"/> for other kinds.</summary>
    public WeekDays Days { get; private init; }

    /// <summary>
    /// Month of <see cref="RepeatKind.Yearly"/>; 0 for other kinds.
    /// Stored, not taken from the due date: a task moved to the next day must still come back to its own date.
    /// </summary>
    public int AnchorMonth { get; private init; }

    /// <summary>
    /// Day of month of <see cref="RepeatKind.Monthly"/> and <see cref="RepeatKind.Yearly"/>; 0 for other kinds.
    /// The 31st falls on the 30th in April and returns to the 31st in May.
    /// </summary>
    public int AnchorDay { get; private init; }

    public static RepeatRule Daily() => new() { Kind = RepeatKind.Daily, Interval = 1 };

    public static RepeatRule EveryNDays(int interval)
    {
        if (interval is < MinInterval or > MaxInterval)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, $"Must be from {MinInterval} to {MaxInterval}.");

        return new RepeatRule { Kind = RepeatKind.EveryNDays, Interval = interval };
    }

    public static RepeatRule Weekly(WeekDays days)
    {
        const WeekDays allDays = (WeekDays)0b111_1111;
        if (days == WeekDays.None || (days & ~allDays) != 0)
            throw new ArgumentException("Choose at least one weekday.", nameof(days));

        return new RepeatRule { Kind = RepeatKind.Weekly, Days = days };
    }

    public static RepeatRule Monthly(int anchorDay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(anchorDay, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anchorDay, 31);

        return new RepeatRule { Kind = RepeatKind.Monthly, AnchorDay = anchorDay };
    }

    public static RepeatRule Yearly(int anchorMonth, int anchorDay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(anchorMonth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anchorMonth, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(anchorDay, 1);

        // A leap year: the 29th of February is a real yearly date
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anchorDay, DateTime.DaysInMonth(2024, anchorMonth));

        return new RepeatRule { Kind = RepeatKind.Yearly, AnchorMonth = anchorMonth, AnchorDay = anchorDay };
    }

    /// <summary>
    /// The first occurrence strictly after <paramref name="date"/>. Monthly and yearly rules look for their
    /// anchor day, so a task done a day late does not skip the occurrence of the current month or year.
    /// </summary>
    public DateOnly NextAfter(DateOnly date) => Kind switch
    {
        RepeatKind.Daily => date.AddDays(1),
        RepeatKind.EveryNDays => date.AddDays(Interval),
        RepeatKind.Weekly => NextWeekdayAfter(date),
        RepeatKind.Monthly => NextMonthDayAfter(date),
        RepeatKind.Yearly => NextYearDayAfter(date),
        _ => throw new InvalidOperationException($"Unknown repeat kind {Kind}.")
    };

    /// <summary>
    /// <paramref name="date"/> itself when it fits the rule, otherwise the nearest occurrence after it:
    /// "Mon, Wed, Fri" chosen on a Tuesday starts on Wednesday.
    /// </summary>
    public DateOnly FirstOnOrAfter(DateOnly date) =>
        Kind is RepeatKind.Daily or RepeatKind.EveryNDays ? date : NextAfter(date.AddDays(-1));

    /// <summary>
    /// Occurrences from <paramref name="first"/> (itself an occurrence, e.g. the task's due date)
    /// that fall between <paramref name="from"/> and <paramref name="to"/>, both included.
    /// </summary>
    public IEnumerable<DateOnly> OccurrencesBetween(DateOnly first, DateOnly from, DateOnly to)
    {
        for (var date = first; date <= to; date = NextAfter(date))
        {
            if (date >= from)
                yield return date;
        }
    }

    private DateOnly NextWeekdayAfter(DateOnly date)
    {
        for (var offset = 1; offset <= 7; offset++)
        {
            var candidate = date.AddDays(offset);
            if (Days.Contains(candidate.DayOfWeek))
                return candidate;
        }

        throw new InvalidOperationException("A weekly rule without weekdays.");
    }

    private DateOnly NextMonthDayAfter(DateOnly date)
    {
        var thisMonth = DueDates.OnDay(date.Year, date.Month, AnchorDay);
        if (thisMonth > date)
            return thisMonth;

        var nextMonth = date.AddMonths(1);
        return DueDates.OnDay(nextMonth.Year, nextMonth.Month, AnchorDay);
    }

    private DateOnly NextYearDayAfter(DateOnly date)
    {
        var thisYear = DueDates.OnDay(date.Year, AnchorMonth, AnchorDay);
        return thisYear > date ? thisYear : DueDates.OnDay(date.Year + 1, AnchorMonth, AnchorDay);
    }
}
