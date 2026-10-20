using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

public enum SnoozeOption
{
    /// <summary>Remind again in 15 minutes.</summary>
    FifteenMinutes,

    /// <summary>Remind again in an hour.</summary>
    OneHour,

    /// <summary>Move the task to tomorrow at the same time.</summary>
    Tomorrow
}

/// <summary>
/// The buttons of a reminder. Returns the local moment of the next reminder; null when there is none,
/// e.g. a task without a reminder moved to tomorrow.
/// </summary>
public record SnoozeTaskCommand(long ChatId, Guid TaskId, SnoozeOption Option) : IRequest<DateTime?>;

public class SnoozeTaskCommandHandler : IRequestHandler<SnoozeTaskCommand, DateTime?>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SnoozeTaskCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<DateTime?> Handle(SnoozeTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        var localNow = _time.GetLocalNow().DateTime;

        switch (request.Option)
        {
            case SnoozeOption.FifteenMinutes:
                task.Snooze(localNow.AddMinutes(15));
                break;
            case SnoozeOption.OneHour:
                task.Snooze(localNow.AddHours(1));
                break;
            case SnoozeOption.Tomorrow:
                task.Schedule(_time.GetLocalToday().AddDays(1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Option, "Unknown snooze option.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task.GetReminderTime();
    }
}
