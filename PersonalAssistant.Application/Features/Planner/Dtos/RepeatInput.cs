using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <summary>
/// A repeat as the user chose it. Monthly and yearly repeats take their day from the task's date,
/// so the user never picks it twice.
/// </summary>
/// <param name="Interval">Only for <see cref="RepeatKind.EveryNDays"/>.</param>
public record RepeatInput(RepeatKind Kind, int Interval = 0)
{
    /// <param name="start">The task's date, or today for a task without one.</param>
    internal RepeatRule ToRule(DateOnly start) => Kind switch
    {
        RepeatKind.Daily => RepeatRule.Daily(),
        RepeatKind.EveryNDays => RepeatRule.EveryNDays(Interval),
        RepeatKind.Monthly => RepeatRule.Monthly(start.Day),
        RepeatKind.Yearly => RepeatRule.Yearly(start.Month, start.Day),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown repeat kind.")
    };
}
