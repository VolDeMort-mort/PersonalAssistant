using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Infrastructure.Persistence;

public class PlannerTopicRepository : IPlannerTopicRepository
{
    private readonly AppDbContext _context;

    public PlannerTopicRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(PlannerTopic topic) => _context.PlannerTopics.Add(topic);
    public void Remove(PlannerTopic topic) => _context.PlannerTopics.Remove(topic);

    public Task<PlannerTopic?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.PlannerTopics.FirstOrDefaultAsync(t => t.ChatId == chatId && t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PlannerTopic>> GetAllAsync(long chatId, CancellationToken cancellationToken) =>
        await _context.PlannerTopics
            .AsNoTracking()
            .Where(t => t.ChatId == chatId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
}
