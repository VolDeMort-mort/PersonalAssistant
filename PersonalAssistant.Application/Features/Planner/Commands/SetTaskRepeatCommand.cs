using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>Null makes it a one-time task. A task without a date starts repeating today.</summary>
public record SetTaskRepeatCommand(long ChatId, Guid TaskId, RepeatInput? Repeat) : IRequest;

public class SetTaskRepeatCommandHandler : IRequestHandler<SetTaskRepeatCommand>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SetTaskRepeatCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task Handle(SetTaskRepeatCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        var today = _time.GetLocalToday();

        task.SetRepeat(request.Repeat?.ToRule(task.DueDate ?? today), today);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
