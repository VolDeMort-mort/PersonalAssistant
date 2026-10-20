namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <summary>A day of the calendar.</summary>
/// <param name="DuePayments">Planned expenses due on this date; overdue ones when the date is already past.</param>
/// <param name="Transactions">Newest first, adjustments included.</param>
/// <param name="Income">Without adjustments, like the month totals.</param>
/// <param name="Expense">Without adjustments, like the month totals.</param>
public record FinanceDayDto(
    DateOnly Date,
    IReadOnlyList<ScheduledPaymentDto> DuePayments,
    IReadOnlyList<TransactionDto> Transactions,
    long Income,
    long Expense);
