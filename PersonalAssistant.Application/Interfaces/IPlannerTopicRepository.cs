using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPlannerTopicRepository
{
    void Add(PlannerTopic topic);
    void Remove(PlannerTopic topic);

    Task<PlannerTopic?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken);

    /// <summary>In the user's order.</summary>
    Task<IReadOnlyList<PlannerTopic>> GetAllAsync(long chatId, CancellationToken cancellationToken);
}
