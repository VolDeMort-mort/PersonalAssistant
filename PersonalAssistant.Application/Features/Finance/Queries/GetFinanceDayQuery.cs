using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>A day of the calendar: planned expenses due on it, and its income and expenses.</summary>
public record GetFinanceDayQuery(long ChatId, DateOnly Date) : IRequest<FinanceDayDto>;

public class GetFinanceDayQueryHandler : IRequestHandler<GetFinanceDayQuery, FinanceDayDto>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly TimeProvider _time;

    public GetFinanceDayQueryHandler(IScheduledPaymentRepository payments, IFinanceTransactionRepository transactions,
        IFinanceCatalogRepository catalog, TimeProvider time)
    {
        _payments = payments;
        _transactions = transactions;
        _catalog = catalog;
        _time = time;
    }

    public async Task<FinanceDayDto> Handle(GetFinanceDayQuery request, CancellationToken cancellationToken)
    {
        var day = _time.GetDay(request.Date);

        var payments = await _payments.GetActiveAsync(request.ChatId, cancellationToken);
        var due = payments.Where(p => p.DueDatesBetween(day.Date, day.Date).Any());
        var duePayments = await _catalog.ToDtosAsync(request.ChatId, due, _time.GetLocalToday(), cancellationToken);

        var transactions = await _transactions.GetForPeriodAsync(request.ChatId, day.FromUtc, day.ToUtc, cancellationToken);
        var items = await _catalog.ToDtosAsync(request.ChatId, transactions, _time, cancellationToken);

        // Adjustments only fix the balance: like the month totals, the day's totals leave them out
        var counted = transactions.Where(t => !t.IsAdjustment).ToList();
        long TotalOf(TransactionType type) => counted.Where(t => t.Type == type).Sum(t => t.Amount);

        return new FinanceDayDto(day.Date, duePayments, items, TotalOf(TransactionType.Income), TotalOf(TransactionType.Expense));
    }
}
