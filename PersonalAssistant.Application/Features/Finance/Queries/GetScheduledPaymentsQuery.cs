using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Payments still to be paid, nearest first (overdue ones come first by date).</summary>
public record GetScheduledPaymentsQuery(long ChatId) : IRequest<IReadOnlyList<ScheduledPaymentDto>>;

public class GetScheduledPaymentsQueryHandler : IRequestHandler<GetScheduledPaymentsQuery, IReadOnlyList<ScheduledPaymentDto>>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetScheduledPaymentsQueryHandler(IScheduledPaymentRepository payments, IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _payments = payments;
        _catalog = catalog;
        _time = time;
    }

    public async Task<IReadOnlyList<ScheduledPaymentDto>> Handle(GetScheduledPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _payments.GetActiveAsync(request.ChatId, cancellationToken);
        return await _catalog.ToDtosAsync(request.ChatId, payments, _time.GetLocalToday(), cancellationToken);
    }
}
