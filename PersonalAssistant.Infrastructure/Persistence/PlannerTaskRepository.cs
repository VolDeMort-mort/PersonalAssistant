using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Infrastructure.Persistence;

public class PlannerTaskRepository : IPlannerTaskRepository
{
    private readonly AppDbContext _context;

    public PlannerTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(PlannerTask task) => _context.PlannerTasks.Add(task);
    public void Remove(PlannerTask task) => _context.PlannerTasks.Remove(task);

    public Task<PlannerTask?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.PlannerTasks.FirstOrDefaultAsync(t => t.ChatId == chatId && t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PlannerTask>> GetOpenDueUntilAsync(long chatId, DateOnly until, CancellationToken cancellationToken) =>
        await Open(chatId)
            .Where(t => t.DueDate != null && t.DueDate <= until)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlannerTask>> GetOpenUndatedAsync(long chatId, CancellationToken cancellationToken) =>
        await Open(chatId)
            .Where(t => t.DueDate == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlannerTask>> GetOpenByTopicAsync(long chatId, Guid topicId, CancellationToken cancellationToken) =>
        await Open(chatId)
            .Where(t => t.TopicId == topicId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountOpenByTopicAsync(long chatId, CancellationToken cancellationToken) =>
        await Open(chatId)
            .Where(t => t.TopicId != null)
            .GroupBy(t => t.TopicId!.Value)
            .Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TopicId, x => x.Count, cancellationToken);

    // Tracked: the caller takes them out of the topic
    public async Task<IReadOnlyList<PlannerTask>> GetAllByTopicAsync(long chatId, Guid topicId, CancellationToken cancellationToken) =>
        await _context.PlannerTasks
            .Where(t => t.ChatId == chatId && t.TopicId == topicId)
            .ToListAsync(cancellationToken);

    // Tracked: the caller marks them as reminded
    public async Task<IReadOnlyList<PlannerTask>> GetReminderCandidatesAsync(DateOnly dueUntil, CancellationToken cancellationToken) =>
        await _context.PlannerTasks
            .Where(t => t.CompletedAt == null
                && (t.SnoozedUntil != null
                    || (t.RemindMinutesBefore != null && t.DueTime != null && t.DueDate != null && t.DueDate <= dueUntil)))
            .ToListAsync(cancellationToken);

    // Tracked: the caller moves them to today
    public async Task<IReadOnlyList<PlannerTask>> GetOverdueAsync(DateOnly today, CancellationToken cancellationToken) =>
        await _context.PlannerTasks
            .Where(t => t.CompletedAt == null && t.DueDate != null && t.DueDate < today)
            .ToListAsync(cancellationToken);

    private IQueryable<PlannerTask> Open(long chatId) =>
        _context.PlannerTasks
            .AsNoTracking()
            .Where(t => t.ChatId == chatId && t.CompletedAt == null);
}
