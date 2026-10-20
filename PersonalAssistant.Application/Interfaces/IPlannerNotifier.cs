using PersonalAssistant.Application.Features.Planner.Dtos;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Port: tells the user about tasks. Application decides when and about what,
/// the outer layer decides how (a Telegram message with buttons today).
/// </summary>
public interface IPlannerNotifier
{
    Task SendReminderAsync(TaskReminderDto reminder, CancellationToken cancellationToken);

    Task SendAgendaAsync(DailyAgendaDto agenda, CancellationToken cancellationToken);
}
