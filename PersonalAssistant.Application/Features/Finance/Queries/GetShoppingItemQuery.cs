using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>The card of an item on the list. Null when it is gone or already bought.</summary>
public record GetShoppingItemQuery(long ChatId, Guid ItemId) : IRequest<ShoppingItemDto?>;

public class GetShoppingItemQueryHandler : IRequestHandler<GetShoppingItemQuery, ShoppingItemDto?>
{
    private readonly IShoppingListRepository _items;
    private readonly IFinanceCatalogRepository _catalog;

    public GetShoppingItemQueryHandler(IShoppingListRepository items, IFinanceCatalogRepository catalog)
    {
        _items = items;
        _catalog = catalog;
    }

    public async Task<ShoppingItemDto?> Handle(GetShoppingItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ChatId, request.ItemId, cancellationToken);
        if (item is null || item.IsBought)
            return null;

        var category = item.CategoryId is { } categoryId
            ? await _catalog.GetCategoryAsync(request.ChatId, categoryId, cancellationToken)
            : null;

        return ShoppingItemDto.From(item, category?.Name);
    }
}
