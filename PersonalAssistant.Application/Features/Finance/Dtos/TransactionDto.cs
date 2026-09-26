using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="CategoryName">Null for balance adjustments.</param>
/// <param name="CreatedAtLocal">In the user's time zone.</param>
/// <param name="IsScheduledPayment">Paid a planned expense: deleting it may move that payment back.</param>
public record TransactionDto(
    Guid Id,
    TransactionType Type,
    long Amount,
    string? CategoryName,
    string? Comment,
    bool IsAdjustment,
    DateTime CreatedAtLocal,
    bool IsScheduledPayment)
{
    internal static TransactionDto From(FinanceTransaction transaction, string? categoryName, DateTime createdAtLocal) => new(
        transaction.Id,
        transaction.Type,
        transaction.Amount,
        categoryName,
        transaction.Comment,
        transaction.IsAdjustment,
        createdAtLocal,
        transaction.ScheduledPaymentId is not null);
}
