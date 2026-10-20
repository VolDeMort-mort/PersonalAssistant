using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="CategoryName">Null for balance adjustments.</param>
/// <param name="CreatedAtLocal">In the user's time zone.</param>
/// <param name="IsScheduledPayment">Paid a planned expense: deleting it may move that payment back.</param>
/// <param name="IsShoppingPurchase">Bought an item of the shopping list: deleting it puts the item back on the list.</param>
public record TransactionDto(
    Guid Id,
    TransactionType Type,
    long Amount,
    string? CategoryName,
    string? Comment,
    bool IsAdjustment,
    DateTime CreatedAtLocal,
    bool IsScheduledPayment,
    bool IsShoppingPurchase)
{
    internal static TransactionDto From(FinanceTransaction transaction, string? categoryName, DateTime createdAtLocal) => new(
        transaction.Id,
        transaction.Type,
        transaction.Amount,
        categoryName,
        transaction.Comment,
        transaction.IsAdjustment,
        createdAtLocal,
        transaction.ScheduledPaymentId is not null,
        transaction.ShoppingItemId is not null);
}
