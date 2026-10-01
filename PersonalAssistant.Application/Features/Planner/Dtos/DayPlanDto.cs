namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <param name="Tasks">Tasks with a time by time, then the rest.</param>
public record DayPlanDto(DateOnly Date, IReadOnlyList<TaskDto> Tasks);
