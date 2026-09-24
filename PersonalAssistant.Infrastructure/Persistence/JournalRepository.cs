using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Infrastructure.Persistence;

public class JournalRepository : IJournalRepository
{
    private readonly AppDbContext _context;

    public JournalRepository(AppDbContext context)
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
    public async Task<JournalEntry?> GetByIdAsync(Guid entryId, CancellationToken cancellationToken)
    {
        return await _context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken);
    }        
    
    public async Task<IEnumerable<JournalEntry>> GetUnprocessedMediaAsync(CancellationToken cancellationToken)
    {
        return await _context.JournalEntries
            .Where(e => e.Status == ProcessingStatus.Pending || e.Status == ProcessingStatus.Downloaded)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<JournalEntry> entries, CancellationToken cancellationToken = default)
    {
        await _context.JournalEntries.AddRangeAsync(entries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        _context.JournalEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);

    }
}