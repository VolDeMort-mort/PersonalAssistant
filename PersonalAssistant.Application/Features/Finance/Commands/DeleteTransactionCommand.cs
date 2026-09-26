using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Undo and deletion from history. The balance follows automatically: it is a sum of transactions.
/// Undoing a scheduled payment makes it due on its date again.
/// Returns false when the transaction is already gone.
/// </summary>
public record DeleteTransactionCommand(long ChatId, Guid TransactionId) : IRequest<bool>;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, bool>
{
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IScheduledPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTransactionCommandHandler(IFinanceTransactionRepository transactions, IScheduledPaymentRepository payments,
        IUnitOfWork unitOfWork)
    {
        _transactions = transactions;
        _payments = payments;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _transactions.GetByIdAsync(request.ChatId, request.TransactionId, cancellationToken);
        if (transaction is null)
            return false;

        if (transaction is { ScheduledPaymentId: { } paymentId, PaidForDate: { } paidForDate })
        {
            var payment = await _payments.GetByIdAsync(request.ChatId, paymentId, cancellationToken);
            payment?.RevertPayment(paidForDate);
        }

        _transactions.Remove(transaction);

        // The removal and the payment's date go together or not at all
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
