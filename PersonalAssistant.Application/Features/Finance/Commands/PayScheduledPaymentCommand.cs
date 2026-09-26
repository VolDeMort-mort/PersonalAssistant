using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// "✅ Оплачено": records the expense and moves the payment to its next date. Returns the transaction id.
/// </summary>
/// <param name="Amount">Amount actually paid; null pays the expected amount.</param>
public record PayScheduledPaymentCommand(long ChatId, Guid PaymentId, long? Amount = null) : IRequest<Guid>;

public class PayScheduledPaymentCommandHandler : IRequestHandler<PayScheduledPaymentCommand, Guid>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public PayScheduledPaymentCommandHandler(IScheduledPaymentRepository payments, IFinanceCatalogRepository catalog,
        IFinanceTransactionRepository transactions, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _payments = payments;
        _catalog = catalog;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(PayScheduledPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetByIdAsync(request.ChatId, request.PaymentId, cancellationToken);

        // A finished one-time payment is not "to be paid" any more: e.g. a button of an old reminder
        if (payment is not { IsActive: true })
            throw new NotFoundException(nameof(ScheduledPayment), request.PaymentId);

        var category = await _catalog.GetCategoryAsync(request.ChatId, payment.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), payment.CategoryId);

        var transaction = payment.Pay(category, request.Amount ?? payment.Amount, _time.GetUtcNow().UtcDateTime);
        _transactions.Add(transaction);

        // The expense and the payment's new date are saved together or not at all
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
