using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>What is still to buy, grouped by expense category in the user's order, with the totals.</summary>
public record GetShoppingListQuery(long ChatId) : IRequest<ShoppingListDto>;

public class GetShoppingListQueryHandler : IRequestHandler<GetShoppingListQuery, ShoppingListDto>
{
    private readonly IShoppingListRepository _items;
    private readonly IFinanceCatalogRepository _catalog;

    public GetShoppingListQueryHandler(IShoppingListRepository items, IFinanceCatalogRepository catalog)
    {
        _items = items;
        _catalog = catalog;
    }

    public async Task<ShoppingListDto> Handle(GetShoppingListQuery request, CancellationToken cancellationToken)
    {
        var items = await _items.GetPendingAsync(request.ChatId, cancellationToken);
        var categories = await _catalog.GetCategoriesAsync(request.ChatId, TransactionType.Expense, cancellationToken);

        var positions = categories.Select((category, index) => (category.Id, index)).ToDictionary(c => c.Id, c => c.index);
        var names = categories.ToDictionary(c => c.Id, c => c.Name);
        int PositionOf(ShoppingItem item) =>
            item.CategoryId is { } id && positions.TryGetValue(id, out var position) ? position : int.MaxValue;

        var sorted = items
            .OrderBy(PositionOf)
            .ThenBy(i => i.Title, StringComparer.CurrentCulture)
            .Select(i => ShoppingItemDto.From(i, i.CategoryId is { } id ? names.GetValueOrDefault(id) : null))
            .ToList();

        // Grouping keeps the order of the first item of each group, which is the order of the categories
        var groupTotals = sorted
            .GroupBy(i => i.CategoryName)
            .Where(group => group.Any(i => i.Price is not null))
            .Select(group => new ShoppingGroupTotalDto(group.Key, group.Sum(i => i.Price ?? 0)))
            .ToList();

        return new ShoppingListDto(
            sorted,
            sorted.Sum(i => i.Price ?? 0),
            sorted.Count(i => i.Price is null),
            groupTotals);
    }
}
