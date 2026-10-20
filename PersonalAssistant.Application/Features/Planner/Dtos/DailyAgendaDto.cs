namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <summary>The morning list: today's tasks, overdue ones included.</summary>
public record DailyAgendaDto(long ChatId, DateOnly Date, IReadOnlyList<TaskDto> Tasks);
