namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="Month">First day of the current month in the user's time zone.</param>
/// <param name="Balance">All money right now, adjustments included.</param>
/// <param name="NextPayment">Nearest scheduled payment that is not overdue yet.</param>
public record FinanceDashboardDto(
    DateOnly Month,
    long Balance,
    long MonthIncome,
    long MonthExpense,
    ScheduledPaymentDto? NextPayment,
    IReadOnlyList<ScheduledPaymentDto> OverduePayments)
{
    /// <summary>Positive: the month is in plus.</summary>
    public long MonthResult => MonthIncome - MonthExpense;
}
