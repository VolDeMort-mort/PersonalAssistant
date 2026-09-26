using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

public record GetFinanceDashboardQuery(long ChatId) : IRequest<FinanceDashboardDto>;

public class GetFinanceDashboardQueryHandler : IRequestHandler<GetFinanceDashboardQuery, FinanceDashboardDto>
{
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IScheduledPaymentRepository _payments;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetFinanceDashboardQueryHandler(IFinanceTransactionRepository transactions, IScheduledPaymentRepository payments,
        IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _transactions = transactions;
        _payments = payments;
        _catalog = catalog;
        _time = time;
    }

    public async Task<FinanceDashboardDto> Handle(GetFinanceDashboardQuery request, CancellationToken cancellationToken)
    {
        var month = _time.GetCurrentMonth();

        var balance = await _transactions.GetBalanceAsync(request.ChatId, cancellationToken);
        var totals = await _transactions.GetTotalsAsync(request.ChatId, month.FromUtc, month.ToUtc, cancellationToken);

        var active = await _payments.GetActiveAsync(request.ChatId, cancellationToken);
        var payments = await _catalog.ToDtosAsync(request.ChatId, active, _time.GetLocalToday(), cancellationToken);

        return new FinanceDashboardDto(
            month.Month,
            balance,
            totals.Income,
            totals.Expense,
            NextPayment: payments.FirstOrDefault(p => !p.IsOverdue),
            OverduePayments: payments.Where(p => p.IsOverdue).ToList());
    }
}
