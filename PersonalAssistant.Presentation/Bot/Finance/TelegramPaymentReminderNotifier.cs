using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>
/// Adapter of the Application port: a reminder becomes a Telegram message with buttons.
/// It lives in Presentation because the buttons and their payloads are the bot's UI.
/// </summary>
public class TelegramPaymentReminderNotifier : IPaymentReminderNotifier
{
    private readonly IBotMessenger _messenger;

    public TelegramPaymentReminderNotifier(IBotMessenger messenger)
    {
        _messenger = messenger;
    }

    public async Task SendAsync(PaymentReminderDto reminder, CancellationToken cancellationToken)
    {
        var screen = PaymentView.Reminder(reminder);

        // A separate message, not the window: only a new message makes the phone notify
        await _messenger.SendAsync(reminder.ChatId, screen.Text, screen.Keyboard, cancellationToken);
    }
}
