using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence;

public class ScheduledPaymentRepository : IScheduledPaymentRepository
{
    private readonly AppDbContext _context;

    public ScheduledPaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(ScheduledPayment payment) => _context.ScheduledPayments.Add(payment);
    public void Remove(ScheduledPayment payment) => _context.ScheduledPayments.Remove(payment);

    public Task<ScheduledPayment?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.ScheduledPayments.FirstOrDefaultAsync(p => p.ChatId == chatId && p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ScheduledPayment>> GetActiveAsync(long chatId, CancellationToken cancellationToken) =>
        await _context.ScheduledPayments
            .AsNoTracking()
            .Where(p => p.ChatId == chatId && p.IsActive)
            .OrderBy(p => p.NextDueDate)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);

    // Tracked: the caller marks them as reminded
    public async Task<IReadOnlyList<ScheduledPayment>> GetReminderCandidatesAsync(DateOnly dueUntil, CancellationToken cancellationToken) =>
        await _context.ScheduledPayments
            .Where(p => p.IsActive
                && p.RemindDaysBefore != null
                && p.NextDueDate <= dueUntil
                && (p.LastRemindedFor == null || p.LastRemindedFor != p.NextDueDate))
            .ToListAsync(cancellationToken);
}
