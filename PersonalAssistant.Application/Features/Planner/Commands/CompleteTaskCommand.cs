using MediatR;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// "✅": a one-time task is done, a repeated one moves on to its next date.
/// Returns what undo needs.
/// </summary>
public record CompleteTaskCommand(long ChatId, Guid TaskId) : IRequest<CompletionDto>;

public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, CompletionDto>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public CompleteTaskCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<CompletionDto> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        var completedDate = task.Complete(_time.GetUtcNow().UtcDateTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompletionDto(task.Id, task.Title, completedDate, task.IsDone ? null : task.DueDate);
    }
}
