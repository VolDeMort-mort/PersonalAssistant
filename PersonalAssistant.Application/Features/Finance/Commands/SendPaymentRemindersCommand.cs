using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Sends every reminder whose time has come, in all chats. Run on a timer; returns how many were sent.
/// If sending fails, the reminder is not marked and the next run tries again.
/// </summary>
public record SendPaymentRemindersCommand : IRequest<int>;

public class SendPaymentRemindersCommandHandler : IRequestHandler<SendPaymentRemindersCommand, int>
{
    private readonly IScheduledPaymentRepository _payments;
    private readonly IPaymentReminderNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SendPaymentRemindersCommandHandler(IScheduledPaymentRepository payments, IPaymentReminderNotifier notifier,
        IUnitOfWork unitOfWork, TimeProvider time)
    {
        _payments = payments;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<int> Handle(SendPaymentRemindersCommand request, CancellationToken cancellationToken)
    {
        var localNow = _time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(localNow);

        // A reminder is at most MaxRemindDaysBefore days early, so later payments can't be due yet
        var candidates = await _payments.GetReminderCandidatesAsync(
            today.AddDays(ScheduledPayment.MaxRemindDaysBefore), cancellationToken);

        var sent = 0;
        foreach (var payment in candidates.Where(p => p.IsReminderDue(localNow)))
        {
            await _notifier.SendAsync(new PaymentReminderDto(
                payment.ChatId,
                payment.Id,
                payment.Title,
                payment.Amount,
                payment.NextDueDate,
                payment.NextDueDate.DayNumber - today.DayNumber), cancellationToken);

            // Saved right after sending: a failure later in the loop must not repeat this reminder
            payment.MarkReminded();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            sent++;
        }

        return sent;
    }
}
