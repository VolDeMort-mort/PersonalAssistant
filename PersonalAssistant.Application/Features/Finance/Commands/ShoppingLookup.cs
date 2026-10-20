using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>Loading what a shopping list command works on.</summary>
internal static class ShoppingLookup
{
    /// <summary>An item still on the list: a bought one is a record of its purchase and is not changed.</summary>
    public static async Task<ShoppingItem> GetPendingItemAsync(this IShoppingListRepository items, long chatId, Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(chatId, itemId, cancellationToken);
        return item is { IsBought: false } ? item : throw new NotFoundException(nameof(ShoppingItem), itemId);
    }

    /// <summary>A null id means "no category".</summary>
    public static async Task<FinanceCategory?> GetCategoryOrNoneAsync(this IFinanceCatalogRepository catalog, long chatId,
        Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is not { } id)
            return null;

        return await catalog.GetCategoryAsync(chatId, id, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), id);
    }
}
