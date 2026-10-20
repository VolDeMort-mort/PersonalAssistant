using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>
/// A month for the calendar: on which days planned expenses are due or overdue, when they were paid,
/// and which days had income or expenses.
/// </summary>
/// <param name="Month">Any day of the month.</param>
public record GetPaymentCalendarQuery(long ChatId, DateOnly Month) : IRequest<PaymentCalendarDto>;

public class GetPaymentCalendarQueryHandler : IRequestHandler<GetPaymentCalendarQuery, PaymentCalendarDto>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly TimeProvider _time;

    public GetPaymentCalendarQueryHandler(IScheduledPaymentRepository payments, IFinanceTransactionRepository transactions,
        TimeProvider time)
    {
        _payments = payments;
        _transactions = transactions;
        _time = time;
    }

    public async Task<PaymentCalendarDto> Handle(GetPaymentCalendarQuery request, CancellationToken cancellationToken)
    {
        var month = _time.GetMonth(request.Month);
        var today = _time.GetLocalToday();

        // A payment is stored with its next date only: the domain works out the rest of its dates in the month
        var payments = await _payments.GetActiveAsync(request.ChatId, cancellationToken);
        var dueDates = payments.SelectMany(p => p.DueDatesBetween(month.Month, month.LastDay)).ToHashSet();

        var transactions = await _transactions.GetForPeriodAsync(request.ChatId, month.FromUtc, month.ToUtc, cancellationToken);
        var byDay = transactions.ToLookup(t => DateOnly.FromDateTime(_time.ToLocal(t.CreatedAt)));

        var days = Enumerable.Range(0, month.LastDay.Day)
            .Select(offset => month.Month.AddDays(offset))
            .Select(date => new CalendarDayDto(
                date,
                HasOverdue: dueDates.Contains(date) && date < today,
                HasDue: dueDates.Contains(date) && date >= today,
                // On the day it was actually paid, which is what the transaction remembers
                HasPaid: byDay[date].Any(t => t.ScheduledPaymentId is not null),
                HasIncome: byDay[date].Any(t => !t.IsAdjustment && t.Type == TransactionType.Income),
                HasExpenses: byDay[date].Any(t => !t.IsAdjustment && t.Type == TransactionType.Expense)))
            .ToList();

        return new PaymentCalendarDto(month.Month, days);
    }
}
