using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Dtos;

/// <summary>An item still on the shopping list.</summary>
/// <param name="Price">Whole hryvnias; null when not known.</param>
/// <param name="CategoryName">Null for an item without a category.</param>
public record ShoppingItemDto(Guid Id, string Title, long? Price, string? Link, Guid? CategoryId, string? CategoryName)
{
    internal static ShoppingItemDto From(ShoppingItem item, string? categoryName) =>
        new(item.Id, item.Title, item.Price, item.Link, item.CategoryId, categoryName);
}
