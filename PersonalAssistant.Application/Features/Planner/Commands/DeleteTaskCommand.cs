using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>Returns false when the task is already gone.</summary>
public record DeleteTaskCommand(long ChatId, Guid TaskId) : IRequest<bool>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaskCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(request.ChatId, request.TaskId, cancellationToken);
        if (task is null)
            return false;

        _tasks.Remove(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
