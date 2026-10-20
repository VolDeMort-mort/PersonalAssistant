namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <param name="CategoryName">Null for the items without a category.</param>
/// <param name="Total">The known prices of the group.</param>
public record ShoppingGroupTotalDto(string? CategoryName, long Total);

/// <param name="Items">By category in the user's order, then by title; items without a category come last.</param>
/// <param name="Total">The known prices only.</param>
/// <param name="UnpricedCount">Items without a price, which the total leaves out.</param>
/// <param name="GroupTotals">Per category, in the order of <paramref name="Items"/>; groups without any price are left out.</param>
public record ShoppingListDto(
    IReadOnlyList<ShoppingItemDto> Items,
    long Total,
    int UnpricedCount,
    IReadOnlyList<ShoppingGroupTotalDto> GroupTotals);
