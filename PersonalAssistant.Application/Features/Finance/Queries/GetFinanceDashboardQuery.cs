using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

public record GetFinanceDashboardQuery(long ChatId) : IRequest<FinanceDashboardDto>;

public class GetFinanceDashboardQueryHandler : IRequestHandler<GetFinanceDashboardQuery, FinanceDashboardDto>
{
    private readonly IFinanceTransactionRepository _transactions;
    private readonly TimeProvider _time;

    public GetFinanceDashboardQueryHandler(IFinanceTransactionRepository transactions, TimeProvider time)
    {
        _transactions = transactions;
        _time = time;
    }

    public async Task<FinanceDashboardDto> Handle(GetFinanceDashboardQuery request, CancellationToken cancellationToken)
    {
        var month = _time.GetCurrentMonth();

        var balance = await _transactions.GetBalanceAsync(request.ChatId, cancellationToken);
        var totals = await _transactions.GetTotalsAsync(request.ChatId, month.FromUtc, month.ToUtc, cancellationToken);

        return new FinanceDashboardDto(month.Month, balance, totals.Income, totals.Expense);
    }
}
