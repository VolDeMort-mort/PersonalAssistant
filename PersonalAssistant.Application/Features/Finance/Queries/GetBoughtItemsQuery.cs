using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>
/// "✅ Куплене": bought items, the latest purchase first, one page at a time.
/// A page out of range is moved into range.
/// </summary>
/// <param name="Page">1-based.</param>
public record GetBoughtItemsQuery(long ChatId, int Page = 1) : IRequest<BoughtItemsPageDto>;

public class GetBoughtItemsQueryHandler : IRequestHandler<GetBoughtItemsQuery, BoughtItemsPageDto>
{
    private const int PageSize = 10;

    private readonly IShoppingListRepository _items;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetBoughtItemsQueryHandler(IShoppingListRepository items, IFinanceTransactionRepository transactions,
        IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _items = items;
        _transactions = transactions;
        _catalog = catalog;
        _time = time;
    }

    public async Task<BoughtItemsPageDto> Handle(GetBoughtItemsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await _items.CountBoughtAsync(request.ChatId, cancellationToken);
        var pageCount = Math.Max(1, (totalCount + PageSize - 1) / PageSize);
        var page = Math.Clamp(request.Page, 1, pageCount);

        var items = await _items.GetBoughtPageAsync(request.ChatId, (page - 1) * PageSize, PageSize, cancellationToken);
        var dtos = await _transactions.ToBoughtDtosAsync(_catalog, request.ChatId, items, _time, cancellationToken);

        return new BoughtItemsPageDto(dtos, page, pageCount, totalCount);
    }
}
