using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Infrastructure.Persistence;

public class PlannerSettingsRepository : IPlannerSettingsRepository
{
    private readonly AppDbContext _context;

    public PlannerSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(PlannerSettings settings) => _context.PlannerSettings.Add(settings);

    public Task<PlannerSettings?> GetAsync(long chatId, CancellationToken cancellationToken) =>
        _context.PlannerSettings.FirstOrDefaultAsync(s => s.ChatId == chatId, cancellationToken);

    // Tracked: the caller marks the morning list as sent
    public async Task<IReadOnlyList<PlannerSettings>> GetWithAgendaAsync(CancellationToken cancellationToken) =>
        await _context.PlannerSettings
            .Where(s => s.AgendaHour != null)
            .ToListAsync(cancellationToken);
}
