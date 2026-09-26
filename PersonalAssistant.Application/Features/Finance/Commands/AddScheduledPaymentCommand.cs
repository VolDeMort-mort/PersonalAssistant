using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <param name="RemindDaysBefore">Null together with <paramref name="RemindAt"/>: no reminder.</param>
public record AddScheduledPaymentCommand(
    long ChatId,
    Guid CategoryId,
    string Title,
    long Amount,
    Recurrence Recurrence,
    DateOnly FirstDueDate,
    int? RemindDaysBefore,
    TimeOnly? RemindAt) : IRequest<Guid>;

public class AddScheduledPaymentCommandHandler : IRequestHandler<AddScheduledPaymentCommand, Guid>
{
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IScheduledPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public AddScheduledPaymentCommandHandler(IFinanceCatalogRepository catalog, IScheduledPaymentRepository payments,
        IUnitOfWork unitOfWork, TimeProvider time)
    {
        _catalog = catalog;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(AddScheduledPaymentCommand request, CancellationToken cancellationToken)
    {
        // The entity has no clock, so "not in the past" is checked by the use case
        if (request.FirstDueDate < _time.GetLocalToday())
            throw new ArgumentOutOfRangeException(nameof(request.FirstDueDate), request.FirstDueDate, "The first payment can't be in the past.");

        var category = await _catalog.GetCategoryAsync(request.ChatId, request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), request.CategoryId);

        var payment = ScheduledPayment.Create(category, request.Title, request.Amount, request.Recurrence,
            request.FirstDueDate, request.RemindDaysBefore, request.RemindAt, _time.GetUtcNow().UtcDateTime);
        _payments.Add(payment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return payment.Id;
    }
}
