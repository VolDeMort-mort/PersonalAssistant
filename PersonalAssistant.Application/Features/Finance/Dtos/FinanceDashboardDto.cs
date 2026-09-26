namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="Month">First day of the current month in the user's time zone.</param>
/// <param name="Balance">All money right now, adjustments included.</param>
public record FinanceDashboardDto(DateOnly Month, long Balance, long MonthIncome, long MonthExpense)
{
    /// <summary>Positive: the month is in plus.</summary>
    public long MonthResult => MonthIncome - MonthExpense;
}
