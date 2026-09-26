using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Interfaces;

public interface IJournalRepository
{
    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken);
    Task<IEnumerable<JournalEntry>> GetByDateAsync(DateTime date, CancellationToken cancellationToken);
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<JournalEntry>> GetUnprocessedMediaAsync(CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<JournalEntry> entries, CancellationToken cancellationToken = default);
    Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default);
}
