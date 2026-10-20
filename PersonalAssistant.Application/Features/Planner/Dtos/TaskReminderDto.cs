namespace PersonalAssistant.Application.Features.Planner.Dtos;

public record TaskReminderDto(long ChatId, Guid TaskId, string Title, DateOnly? DueDate, TimeOnly? DueTime);
