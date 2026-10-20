namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <param name="Days">Seven days starting today.</param>
public record WeekPlanDto(IReadOnlyList<DayPlanDto> Days);
