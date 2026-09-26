using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Everything the payment card shows. Null when the payment is gone or already finished.</summary>
public record GetScheduledPaymentQuery(long ChatId, Guid PaymentId) : IRequest<ScheduledPaymentDto?>;

public class GetScheduledPaymentQueryHandler : IRequestHandler<GetScheduledPaymentQuery, ScheduledPaymentDto?>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetScheduledPaymentQueryHandler(IScheduledPaymentRepository payments, IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _payments = payments;
        _catalog = catalog;
        _time = time;
    }

    public async Task<ScheduledPaymentDto?> Handle(GetScheduledPaymentQuery request, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetByIdAsync(request.ChatId, request.PaymentId, cancellationToken);
        if (payment is not { IsActive: true })
            return null;

        var dtos = await _catalog.ToDtosAsync(request.ChatId, new[] { payment }, _time.GetLocalToday(), cancellationToken);
        return dtos[0];
    }
}
