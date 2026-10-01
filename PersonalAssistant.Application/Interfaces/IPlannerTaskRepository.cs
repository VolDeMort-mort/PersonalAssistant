using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPlannerTaskRepository
{
    void Add(PlannerTask task);
    void Remove(PlannerTask task);

    /// <summary>Done tasks too: undo needs them.</summary>
    Task<PlannerTask?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken);

    /// <summary>Open tasks with a date up to <paramref name="until"/>, overdue ones included.</summary>
    Task<IReadOnlyList<PlannerTask>> GetOpenDueUntilAsync(long chatId, DateOnly until, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlannerTask>> GetOpenUndatedAsync(long chatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlannerTask>> GetOpenByTopicAsync(long chatId, Guid topicId, CancellationToken cancellationToken);

    /// <summary>Open tasks per topic; topics without open tasks are missing.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountOpenByTopicAsync(long chatId, CancellationToken cancellationToken);

    /// <summary>Done tasks too: a deleted topic has to let go of all of them.</summary>
    Task<IReadOnlyList<PlannerTask>> GetAllByTopicAsync(long chatId, Guid topicId, CancellationToken cancellationToken);

    /// <summary>
    /// All chats: open tasks that are snoozed, or have a reminder and are due no later than <paramref name="dueUntil"/>.
    /// The exact moment is checked by <see cref="PlannerTask.IsReminderDue"/>.
    /// </summary>
    Task<IReadOnlyList<PlannerTask>> GetReminderCandidatesAsync(DateOnly dueUntil, CancellationToken cancellationToken);

    /// <summary>All chats: open tasks due before <paramref name="today"/>.</summary>
    Task<IReadOnlyList<PlannerTask>> GetOverdueAsync(DateOnly today, CancellationToken cancellationToken);
}
