using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Infrastructure.Persistence;

public class MongoDbJournalRepository : IJournalRepository
{
    // Тут логіка підключення до MongoDB (MongoClient, IMongoCollection)

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        // Збереження в Mongo...
    }

    public async Task<IEnumerable<JournalEntry>> GetByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        // Пошук в Mongo...
        return new List<JournalEntry>();
    }
}