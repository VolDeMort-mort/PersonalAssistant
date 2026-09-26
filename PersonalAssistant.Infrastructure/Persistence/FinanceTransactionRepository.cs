using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence;

public class FinanceTransactionRepository : IFinanceTransactionRepository
{
    private readonly AppDbContext _context;

    public FinanceTransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(FinanceTransaction transaction) => _context.FinanceTransactions.Add(transaction);
    public void Remove(FinanceTransaction transaction) => _context.FinanceTransactions.Remove(transaction);

    public Task<FinanceTransaction?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.FinanceTransactions.FirstOrDefaultAsync(t => t.ChatId == chatId && t.Id == id, cancellationToken);

    public Task<long> GetBalanceAsync(long chatId, CancellationToken cancellationToken) =>
        _context.FinanceTransactions
            .Where(t => t.ChatId == chatId)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount, cancellationToken);

    public async Task<PeriodTotals> GetTotalsAsync(long chatId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        // One query: SQL returns a row per type instead of two separate sums
        var sums = await _context.FinanceTransactions
            .Where(t => t.ChatId == chatId && !t.IsAdjustment && t.CreatedAt >= fromUtc && t.CreatedAt < toUtc)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        long TotalOf(TransactionType type) => sums.FirstOrDefault(s => s.Type == type)?.Total ?? 0;

        return new PeriodTotals(TotalOf(TransactionType.Income), TotalOf(TransactionType.Expense));
    }
}
