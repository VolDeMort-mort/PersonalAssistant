using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// Sends the morning list of today's tasks to every chat whose hour has come. Run on a timer;
/// returns how many lists were sent. A day with nothing to do sends nothing and still counts as done.
/// </summary>
public record SendDailyAgendaCommand : IRequest<int>;

public class SendDailyAgendaCommandHandler : IRequestHandler<SendDailyAgendaCommand, int>
{
    private readonly IPlannerSettingsRepository _settings;
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;
    private readonly IPlannerNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SendDailyAgendaCommandHandler(IPlannerSettingsRepository settings, IPlannerTaskRepository tasks,
        IPlannerTopicRepository topics, IPlannerNotifier notifier, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _settings = settings;
        _tasks = tasks;
        _topics = topics;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<int> Handle(SendDailyAgendaCommand request, CancellationToken cancellationToken)
    {
        var localNow = _time.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(localNow);

        var sent = 0;
        foreach (var settings in await _settings.GetWithAgendaAsync(cancellationToken))
        {
            if (!settings.IsAgendaDue(localNow))
                continue;

            var tasks = await _tasks.GetOpenDueUntilAsync(settings.ChatId, today, cancellationToken);
            if (tasks.Count > 0)
            {
                var topicNames = await _topics.GetTopicNamesAsync(settings.ChatId, cancellationToken);
                var agenda = tasks.TasksOn(today, today, topicNames).InDayOrder();

                await _notifier.SendAgendaAsync(new DailyAgendaDto(settings.ChatId, today, agenda), cancellationToken);
                sent++;
            }

            // Saved per chat: a failure in another chat must not send this list twice
            settings.MarkAgendaSent(today);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }
}
