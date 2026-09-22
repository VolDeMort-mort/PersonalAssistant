using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Application.Interfaces;

public interface IJournalRepository
{
    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken);
    Task<IEnumerable<JournalEntry>> GetByDateAsync(DateTime date, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<JournalEntry> entries, CancellationToken cancellationToken = default);

}
