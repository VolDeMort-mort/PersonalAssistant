using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>Another day for the task; null makes it a task without a date (and without time, repeat and reminder).</summary>
public record RescheduleTaskCommand(long ChatId, Guid TaskId, DateOnly? Date) : IRequest;

public class RescheduleTaskCommandHandler : IRequestHandler<RescheduleTaskCommand>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleTaskCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RescheduleTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        task.Schedule(request.Date);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
