namespace PersonalAssistant.Application.Features.Finance.Dtos;

public record CategoryTotalDto(string CategoryName, long Total);

/// <param name="Month">First day of the month in the user's time zone.</param>
/// <param name="Page">1-based; already within 1..<paramref name="PageCount"/>.</param>
/// <param name="TotalCount">All transactions of the month.</param>
/// <param name="MonthExpense">Without adjustments: the base for the top categories' shares.</param>
public record HistoryPageDto(
    DateOnly Month,
    IReadOnlyList<TransactionDto> Transactions,
    int Page,
    int PageCount,
    int TotalCount,
    IReadOnlyList<CategoryTotalDto> TopExpenses,
    long MonthExpense);
