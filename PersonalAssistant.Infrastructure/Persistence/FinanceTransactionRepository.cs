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
        var sums = await InPeriod(chatId, fromUtc, toUtc)
            .Where(t => !t.IsAdjustment)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        long TotalOf(TransactionType type) => sums.FirstOrDefault(s => s.Type == type)?.Total ?? 0;

        return new PeriodTotals(TotalOf(TransactionType.Income), TotalOf(TransactionType.Expense));
    }

    public async Task<IReadOnlyList<FinanceTransaction>> GetPageAsync(long chatId, DateTime fromUtc, DateTime toUtc, int skip, int take,
        CancellationToken cancellationToken) =>
        await InPeriod(chatId, fromUtc, toUtc)
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.Id) // stable pages when several transactions share a time
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(long chatId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        InPeriod(chatId, fromUtc, toUtc).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryTotal>> GetTopExpenseCategoriesAsync(long chatId, DateTime fromUtc, DateTime toUtc, int take,
        CancellationToken cancellationToken)
    {
        // Sorted and cut in SQL on an anonymous type; the record is built afterwards in memory
        var totals = await InPeriod(chatId, fromUtc, toUtc)
            .Where(t => t.Type == TransactionType.Expense && !t.IsAdjustment && t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(c => c.Total)
            .Take(take)
            .ToListAsync(cancellationToken);

        return totals.Select(c => new CategoryTotal(c.CategoryId, c.Total)).ToList();
    }

    private IQueryable<FinanceTransaction> InPeriod(long chatId, DateTime fromUtc, DateTime toUtc) =>
        _context.FinanceTransactions.Where(t => t.ChatId == chatId && t.CreatedAt >= fromUtc && t.CreatedAt < toUtc);
}
