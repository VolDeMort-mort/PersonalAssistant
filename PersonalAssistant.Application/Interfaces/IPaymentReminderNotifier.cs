using PersonalAssistant.Application.Features.Finance.Dtos;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Port: tells the user that a payment is coming up. Application decides when and about what,
/// the outer layer decides how (a Telegram message with buttons today).
/// </summary>
public interface IPaymentReminderNotifier
{
    Task SendAsync(PaymentReminderDto reminder, CancellationToken cancellationToken);
}
