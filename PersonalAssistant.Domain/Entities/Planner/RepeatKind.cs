namespace PersonalAssistant.Domain.Entities.Planner;

public enum RepeatKind
{
    Daily = 0,

    /// <summary>Every <see cref="RepeatRule.Interval"/> days, e.g. watering plants every 3 days.</summary>
    EveryNDays = 1,

    /// <summary>On the <see cref="RepeatRule.Days"/> of every week.</summary>
    Weekly = 2,

    /// <summary>On the <see cref="RepeatRule.AnchorDay"/> of every month.</summary>
    Monthly = 3,

    /// <summary>On the same month and <see cref="RepeatRule.AnchorDay"/> every year.</summary>
    Yearly = 4
}
