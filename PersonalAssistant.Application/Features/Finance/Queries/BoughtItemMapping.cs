using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Bought items → DTOs with their expenses: shared by the history of purchases and its card.</summary>
internal static class BoughtItemMapping
{
    public static async Task<IReadOnlyList<BoughtItemDto>> ToBoughtDtosAsync(this IFinanceTransactionRepository transactions,
        IFinanceCatalogRepository catalog, long chatId, IReadOnlyList<ShoppingItem> items, TimeProvider time,
        CancellationToken cancellationToken)
    {
        var expenses = await transactions.GetByShoppingItemsAsync(chatId, items.Select(i => i.Id).ToList(), cancellationToken);
        var expenseOf = expenses
            .Where(t => t.ShoppingItemId is not null)
            .ToDictionary(t => t.ShoppingItemId!.Value);

        var categories = await catalog.GetCategoriesAsync(chatId, TransactionType.Expense, cancellationToken);
        var names = categories.ToDictionary(c => c.Id, c => c.Name);

        return items
            .Select(item =>
            {
                var expense = expenseOf.GetValueOrDefault(item.Id);
                return new BoughtItemDto(
                    item.Id,
                    item.Title,
                    item.Link,
                    item.CategoryId is { } categoryId ? names.GetValueOrDefault(categoryId) : null,
                    time.ToLocal(item.BoughtAt!.Value),
                    expense?.Amount,
                    expense?.Id);
            })
            .ToList();
    }
}
