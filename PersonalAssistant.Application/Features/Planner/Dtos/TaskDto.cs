using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Dtos;

/// <summary>A task in a list.</summary>
/// <param name="DueDate">The day it is shown on: in the week view a repeated task appears on each of its days.</param>
public record TaskDto(
    Guid Id,
    string Title,
    DateOnly? DueDate,
    TimeOnly? DueTime,
    string? TopicName,
    bool IsRepeated,
    bool HasReminder)
{
    /// <param name="date">Another occurrence of a repeated task; its own due date by default.</param>
    internal static TaskDto From(PlannerTask task, string? topicName, DateOnly? date = null) => new(
        task.Id,
        task.Title,
        date ?? task.DueDate,
        task.DueTime,
        topicName,
        task.Repeat is not null,
        task.RemindMinutesBefore is not null);
}
