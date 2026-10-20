using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>The card of a bought item. Null when it is gone or back on the list.</summary>
public record GetBoughtItemQuery(long ChatId, Guid ItemId) : IRequest<BoughtItemDto?>;

public class GetBoughtItemQueryHandler : IRequestHandler<GetBoughtItemQuery, BoughtItemDto?>
{
    private readonly IShoppingListRepository _items;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetBoughtItemQueryHandler(IShoppingListRepository items, IFinanceTransactionRepository transactions,
        IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _items = items;
        _transactions = transactions;
        _catalog = catalog;
        _time = time;
    }

    public async Task<BoughtItemDto?> Handle(GetBoughtItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ChatId, request.ItemId, cancellationToken);
        if (item is not { IsBought: true })
            return null;

        var dtos = await _transactions.ToBoughtDtosAsync(_catalog, request.ChatId, new[] { item }, _time, cancellationToken);
        return dtos[0];
    }
}
