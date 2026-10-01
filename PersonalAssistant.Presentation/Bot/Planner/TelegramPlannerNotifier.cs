using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>
/// Adapter of the Application port: reminders and the morning list become Telegram messages with buttons.
/// It lives in Presentation because the buttons and their payloads are the bot's UI.
/// </summary>
public class TelegramPlannerNotifier : IPlannerNotifier
{
    private readonly IBotMessenger _messenger;
    private readonly TimeProvider _time;

    public TelegramPlannerNotifier(IBotMessenger messenger, TimeProvider time)
    {
        _messenger = messenger;
        _time = time;
    }

    public Task SendReminderAsync(TaskReminderDto reminder, CancellationToken cancellationToken) =>
        SendAsync(reminder.ChatId, PlannerView.Reminder(reminder, _time.GetLocalToday()), cancellationToken);

    public Task SendAgendaAsync(DailyAgendaDto agenda, CancellationToken cancellationToken) =>
        SendAsync(agenda.ChatId, PlannerView.Agenda(agenda), cancellationToken);

    // A separate message, not the window: only a new message makes the phone notify
    private Task SendAsync(long chatId, BotScreen screen, CancellationToken cancellationToken) =>
        _messenger.SendAsync(chatId, screen.Text, screen.Keyboard, cancellationToken);
}
