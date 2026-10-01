using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <summary>A task's repeat, for showing it: see <see cref="RepeatRule"/> for the meaning of each field.</summary>
public record RepeatDto(RepeatKind Kind, int Interval, WeekDays Days, int AnchorMonth, int AnchorDay)
{
    internal static RepeatDto From(RepeatRule rule) =>
        new(rule.Kind, rule.Interval, rule.Days, rule.AnchorMonth, rule.AnchorDay);
}
