using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Manual income or expense; the category decides which one. Returns the new transaction id.
/// </summary>
public record AddTransactionCommand(long ChatId, Guid CategoryId, long Amount, string? Comment) : IRequest<Guid>;

public class AddTransactionCommandHandler : IRequestHandler<AddTransactionCommand, Guid>
{
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public AddTransactionCommandHandler(IFinanceCatalogRepository catalog, IFinanceTransactionRepository transactions,
        IUnitOfWork unitOfWork, TimeProvider time)
    {
        _catalog = catalog;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(AddTransactionCommand request, CancellationToken cancellationToken)
    {
        var category = await _catalog.GetCategoryAsync(request.ChatId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), request.CategoryId);

        var transaction = FinanceTransaction.Create(category, request.Amount, request.Comment, _time.GetUtcNow().UtcDateTime);
        _transactions.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
