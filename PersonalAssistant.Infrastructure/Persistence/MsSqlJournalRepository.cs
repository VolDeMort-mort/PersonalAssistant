using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Infrastructure.Persistence;

public class MsSqlJournalRepository : IJournalRepository
{
    private readonly AppDbContext _context;

    public MsSqlJournalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        await _context.JournalEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<JournalEntry>> GetByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        return await _context.JournalEntries
            .Where(e => e.CreatedAt.Date == date.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<JournalEntry> entries, CancellationToken cancellationToken = default)
    {
        await _context.JournalEntries.AddRangeAsync(entries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}