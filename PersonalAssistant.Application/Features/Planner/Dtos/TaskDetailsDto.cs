using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <summary>Everything the task card shows.</summary>
/// <param name="RemindMinutesBefore">0 at the task's time; null without a reminder.</param>
public record TaskDetailsDto(
    Guid Id,
    string Title,
    DateOnly? DueDate,
    TimeOnly? DueTime,
    RepeatDto? Repeat,
    int? RemindMinutesBefore,
    Guid? TopicId,
    string? TopicName)
{
    internal static TaskDetailsDto From(PlannerTask task, string? topicName) => new(
        task.Id,
        task.Title,
        task.DueDate,
        task.DueTime,
        task.Repeat is { } rule ? RepeatDto.From(rule) : null,
        task.RemindMinutesBefore,
        task.TopicId,
        topicName);
}
