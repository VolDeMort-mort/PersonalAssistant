using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>Loading what a command works on: shared by the planner commands.</summary>
internal static class PlannerLookup
{
    /// <summary>A task the user can still change: a done one-time task is gone as far as the screens go.</summary>
    public static async Task<PlannerTask> GetOpenTaskAsync(this IPlannerTaskRepository tasks, long chatId, Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(chatId, taskId, cancellationToken);
        return task is { IsDone: false } ? task : throw new NotFoundException(nameof(PlannerTask), taskId);
    }

    /// <summary>A null id means "no topic".</summary>
    public static async Task<PlannerTopic?> GetTopicOrNoneAsync(this IPlannerTopicRepository topics, long chatId, Guid? topicId,
        CancellationToken cancellationToken)
    {
        if (topicId is not { } id)
            return null;

        return await topics.GetByIdAsync(chatId, id, cancellationToken)
            ?? throw new NotFoundException(nameof(PlannerTopic), id);
    }
}
