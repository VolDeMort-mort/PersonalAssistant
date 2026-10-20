using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// Sends every task reminder whose moment has come, in all chats. Run on a timer; returns how many were sent.
/// If sending fails, the reminder is not marked and the next run tries again.
/// </summary>
public record SendTaskRemindersCommand : IRequest<int>;

public class SendTaskRemindersCommandHandler : IRequestHandler<SendTaskRemindersCommand, int>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SendTaskRemindersCommandHandler(IPlannerTaskRepository tasks, IPlannerNotifier notifier, IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _tasks = tasks;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<int> Handle(SendTaskRemindersCommand request, CancellationToken cancellationToken)
    {
        var localNow = _time.GetLocalNow().DateTime;

        // A reminder is at most a day early, so tasks due later can't be due yet
        var dueUntil = DateOnly.FromDateTime(localNow.AddMinutes(PlannerTask.MaxRemindMinutesBefore));
        var candidates = await _tasks.GetReminderCandidatesAsync(dueUntil, cancellationToken);

        var sent = 0;
        foreach (var task in candidates.Where(t => t.IsReminderDue(localNow)))
        {
            await _notifier.SendReminderAsync(
                new TaskReminderDto(task.ChatId, task.Id, task.Title, task.DueDate, task.DueTime), cancellationToken);

            // Saved right after sending: a failure later in the loop must not repeat this reminder
            task.MarkReminded();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            sent++;
        }

        return sent;
    }
}
