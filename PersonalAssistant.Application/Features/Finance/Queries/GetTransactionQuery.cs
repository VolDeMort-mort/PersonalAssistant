using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Null when the transaction is gone.</summary>
public record GetTransactionQuery(long ChatId, Guid TransactionId) : IRequest<TransactionDto?>;

public class GetTransactionQueryHandler : IRequestHandler<GetTransactionQuery, TransactionDto?>
{
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetTransactionQueryHandler(IFinanceTransactionRepository transactions, IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _transactions = transactions;
        _catalog = catalog;
        _time = time;
    }

    public async Task<TransactionDto?> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(request.ChatId, request.TransactionId, cancellationToken);
        if (transaction is null)
            return null;

        var category = transaction.CategoryId is { } categoryId
            ? await _catalog.GetCategoryAsync(request.ChatId, categoryId, cancellationToken)
            : null;

        return TransactionDto.From(transaction, category?.Name, _time.ToLocal(transaction.CreatedAt));
    }
}
