using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPlannerSettingsRepository
{
    void Add(PlannerSettings settings);

    /// <summary>Null until the planner is opened for the first time.</summary>
    Task<PlannerSettings?> GetAsync(long chatId, CancellationToken cancellationToken);

    /// <summary>All chats whose morning list is turned on.</summary>
    Task<IReadOnlyList<PlannerSettings>> GetWithAgendaAsync(CancellationToken cancellationToken);
}
