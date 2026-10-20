using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

public record RenameTaskCommand(long ChatId, Guid TaskId, string Title) : IRequest;

public class RenameTaskCommandHandler : IRequestHandler<RenameTaskCommand>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public RenameTaskCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RenameTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        task.Rename(request.Title);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
