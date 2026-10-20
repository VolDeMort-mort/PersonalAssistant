using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// "↩️ Скасувати" after "✅". Returns false when there is nothing to undo any more:
/// the task is gone, or a repeated one has been completed again since.
/// </summary>
/// <param name="CompletedDate">From <see cref="Dtos.CompletionDto.CompletedDate"/>.</param>
public record ReopenTaskCommand(long ChatId, Guid TaskId, DateOnly? CompletedDate) : IRequest<bool>;

public class ReopenTaskCommandHandler : IRequestHandler<ReopenTaskCommand, bool>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenTaskCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ReopenTaskCommand request, CancellationToken cancellationToken)
    {
        // A done task is exactly what is reopened, so not GetOpenTaskAsync here
        var task = await _tasks.GetByIdAsync(request.ChatId, request.TaskId, cancellationToken);
        if (task is null || !task.Reopen(request.CompletedDate))
            return false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
