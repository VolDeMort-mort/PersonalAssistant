namespace PersonalAssistant.Application.Features.Planner.Dtos;

public record TopicTasksDto(TopicDto Topic, IReadOnlyList<TaskDto> Tasks);
