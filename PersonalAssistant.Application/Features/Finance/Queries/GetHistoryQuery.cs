using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>
/// Transactions of the current month, newest first, one page at a time, with the top expense categories.
/// A page out of range (e.g. after the last item of the last page was deleted) is moved into range.
/// </summary>
/// <param name="Page">1-based.</param>
public record GetHistoryQuery(long ChatId, int Page = 1) : IRequest<HistoryPageDto>;

public class GetHistoryQueryHandler : IRequestHandler<GetHistoryQuery, HistoryPageDto>
{
    private const int PageSize = 10;
    private const int TopCategories = 3;

    private readonly IFinanceTransactionRepository _transactions;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetHistoryQueryHandler(IFinanceTransactionRepository transactions, IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _transactions = transactions;
        _catalog = catalog;
        _time = time;
    }

    public async Task<HistoryPageDto> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
    {
        var month = _time.GetCurrentMonth();

        var totalCount = await _transactions.CountAsync(request.ChatId, month.FromUtc, month.ToUtc, cancellationToken);
        var pageCount = Math.Max(1, (totalCount + PageSize - 1) / PageSize);
        var page = Math.Clamp(request.Page, 1, pageCount);

        var transactions = await _transactions.GetPageAsync(
            request.ChatId, month.FromUtc, month.ToUtc, (page - 1) * PageSize, PageSize, cancellationToken);
        var top = await _transactions.GetTopExpenseCategoriesAsync(
            request.ChatId, month.FromUtc, month.ToUtc, TopCategories, cancellationToken);
        var totals = await _transactions.GetTotalsAsync(request.ChatId, month.FromUtc, month.ToUtc, cancellationToken);

        var categories = await _catalog.GetAllCategoriesAsync(request.ChatId, cancellationToken);
        var names = categories.ToDictionary(c => c.Id, c => c.Name);
        string? NameOf(Guid? categoryId) => categoryId is { } id ? names.GetValueOrDefault(id, "—") : null;

        return new HistoryPageDto(
            month.Month,
            transactions.Select(t => TransactionDto.From(t, NameOf(t.CategoryId), _time.ToLocal(t.CreatedAt))).ToList(),
            page,
            pageCount,
            totalCount,
            top.Select(c => new CategoryTotalDto(NameOf(c.CategoryId)!, c.Total)).ToList(),
            totals.Expense);
    }
}
