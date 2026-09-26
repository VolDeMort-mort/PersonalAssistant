using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>Income and expenses for a period; balance adjustments are not included.</summary>
public record PeriodTotals(long Income, long Expense);

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
}
