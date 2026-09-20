using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities;

namespace PersonalAssistant.Infrastructure.Persistence;

public class MockJournalRepository : IJournalRepository
{
    public Task AddAsync(JournalEntry entry, CancellationToken cancellationToken)
    {
        // Імітуємо збереження в БД
        Console.WriteLine($"\n[DATABASE MOCK] Успішно \"збережено\" запис: {entry.Text}");
        if (entry.EmotionTag != null)
            Console.WriteLine($"[DATABASE MOCK] Емоція: {entry.EmotionTag}");

        return Task.CompletedTask;
    }

    public Task<IEnumerable<JournalEntry>> GetByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<JournalEntry>>(new List<JournalEntry>());
    }
}