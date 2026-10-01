namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <param name="AgendaHour">Hour of the morning list; null when it is turned off.</param>
public record PlannerSettingsDto(int? AgendaHour);
