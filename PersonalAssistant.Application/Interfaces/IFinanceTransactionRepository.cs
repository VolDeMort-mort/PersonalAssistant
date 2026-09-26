using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>Income and expenses for a period; balance adjustments are not included.</summary>
public record PeriodTotals(long Income, long Expense);

/// <summary>How much was spent in one category.</summary>
public record CategoryTotal(Guid CategoryId, long Total);

/// <summary>
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IFinanceTransactionRepository
{
    void Add(FinanceTransaction transaction);
    void Remove(FinanceTransaction transaction);

    Task<FinanceTransaction?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken);

    /// <summary>Sum of all transactions ever made, adjustments included.</summary>
    Task<long> GetBalanceAsync(long chatId, CancellationToken cancellationToken);

    /// <param name="fromUtc">Inclusive.</param>
    /// <param name="toUtc">Exclusive.</param>
    Task<PeriodTotals> GetTotalsAsync(long chatId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    /// <summary>Transactions of the period, newest first, adjustments included.</summary>
    Task<IReadOnlyList<FinanceTransaction>> GetPageAsync(long chatId, DateTime fromUtc, DateTime toUtc, int skip, int take,
        CancellationToken cancellationToken);

    Task<int> CountAsync(long chatId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    /// <summary>Categories with the biggest expenses of the period, biggest first; adjustments are not included.</summary>
    Task<IReadOnlyList<CategoryTotal>> GetTopExpenseCategoriesAsync(long chatId, DateTime fromUtc, DateTime toUtc, int take,
        CancellationToken cancellationToken);
}
