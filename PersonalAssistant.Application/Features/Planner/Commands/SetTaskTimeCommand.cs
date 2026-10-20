using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>Null makes it a task for the whole day, which also removes its reminder.</summary>
public record SetTaskTimeCommand(long ChatId, Guid TaskId, TimeOnly? Time) : IRequest;

public class SetTaskTimeCommandHandler : IRequestHandler<SetTaskTimeCommand>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public SetTaskTimeCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetTaskTimeCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        task.SetTime(request.Time);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
