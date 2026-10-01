using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <param name="MinutesBefore">0 reminds at the task's time; null removes the reminder.</param>
public record SetTaskReminderCommand(long ChatId, Guid TaskId, int? MinutesBefore) : IRequest;

public class SetTaskReminderCommandHandler : IRequestHandler<SetTaskReminderCommand>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public SetTaskReminderCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetTaskReminderCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        task.SetReminder(request.MinutesBefore);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
