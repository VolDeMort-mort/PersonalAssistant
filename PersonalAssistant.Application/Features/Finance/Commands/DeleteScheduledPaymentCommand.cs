using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Payments already made stay in the history. Returns false when the payment is already gone.
/// </summary>
public record DeleteScheduledPaymentCommand(long ChatId, Guid PaymentId) : IRequest<bool>;

public class DeleteScheduledPaymentCommandHandler : IRequestHandler<DeleteScheduledPaymentCommand, bool>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteScheduledPaymentCommandHandler(IScheduledPaymentRepository payments, IUnitOfWork unitOfWork)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteScheduledPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetByIdAsync(request.ChatId, request.PaymentId, cancellationToken);
        if (payment is null)
            return false;

        _payments.Remove(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
