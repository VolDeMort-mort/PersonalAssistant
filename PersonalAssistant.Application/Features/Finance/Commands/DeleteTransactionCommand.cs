using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Undo and deletion from history. The balance follows automatically: it is a sum of transactions.
/// Returns false when the transaction is already gone.
/// </summary>
public record DeleteTransactionCommand(long ChatId, Guid TransactionId) : IRequest<bool>;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, bool>
{
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTransactionCommandHandler(IFinanceTransactionRepository transactions, IUnitOfWork unitOfWork)
    {
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(request.ChatId, request.TransactionId, cancellationToken);
        if (transaction is null)
            return false;

        _transactions.Remove(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
