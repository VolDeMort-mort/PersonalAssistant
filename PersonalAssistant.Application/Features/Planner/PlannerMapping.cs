using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner;

/// <summary>Tasks → list items: shared by the planner queries and the morning list.</summary>
internal static class PlannerMapping
{
    public static async Task<IReadOnlyDictionary<Guid, string>> GetTopicNamesAsync(this IPlannerTopicRepository topics,
        long chatId, CancellationToken cancellationToken)
    {
        var all = await topics.GetAllAsync(chatId, cancellationToken);
        return all.ToDictionary(t => t.Id, t => t.Name);
    }

    public static string? TopicNameOf(this IReadOnlyDictionary<Guid, string> topicNames, PlannerTask task) =>
        task.TopicId is { } id ? topicNames.GetValueOrDefault(id) : null;

    /// <summary>
    /// Tasks shown on <paramref name="date"/>: due that day, and repeated ones that fall on it.
    /// Overdue tasks count as today's, as the worker is about to move them there.
    /// </summary>
    /// <param name="tasks">Open tasks with a date.</param>
    public static IEnumerable<TaskDto> TasksOn(this IEnumerable<PlannerTask> tasks, DateOnly date, DateOnly today,
        IReadOnlyDictionary<Guid, string> topicNames)
    {
        foreach (var task in tasks)
        {
            if (task.DueDate is not { } dueDate)
                continue;

            var shownFrom = dueDate < today ? today : dueDate;
            var fallsOnDate = task.Repeat is { } rule
                ? rule.OccurrencesBetween(shownFrom, date, date).Any()
                : shownFrom == date;

            if (fallsOnDate)
                yield return TaskDto.From(task, topicNames.TopicNameOf(task), date);
        }
    }

    /// <summary>The order of a day: tasks with a time by time, then the rest by title.</summary>
    public static IReadOnlyList<TaskDto> InDayOrder(this IEnumerable<TaskDto> tasks) =>
        tasks
            .OrderBy(t => t.DueTime is null)
            .ThenBy(t => t.DueTime)
            .ThenBy(t => t.Title, StringComparer.CurrentCulture)
            .ToList();

    /// <summary>The order of a list across days: the nearest first, tasks without a date at the end.</summary>
    public static IReadOnlyList<TaskDto> InDateOrder(this IEnumerable<TaskDto> tasks) =>
        tasks
            .OrderBy(t => t.DueDate is null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.DueTime is null)
            .ThenBy(t => t.DueTime)
            .ThenBy(t => t.Title, StringComparer.CurrentCulture)
            .ToList();
}
