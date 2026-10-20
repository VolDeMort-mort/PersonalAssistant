using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// Moves unfinished tasks of past days to today, in all chats. Run on a timer; returns how many moved.
/// Running it again changes nothing, so a missed or repeated run is harmless.
/// </summary>
public record RollOverOverdueTasksCommand : IRequest<int>;

public class RollOverOverdueTasksCommandHandler : IRequestHandler<RollOverOverdueTasksCommand, int>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public RollOverOverdueTasksCommandHandler(IPlannerTaskRepository tasks, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<int> Handle(RollOverOverdueTasksCommand request, CancellationToken cancellationToken)
    {
        var today = _time.GetLocalToday();
        var overdue = await _tasks.GetOverdueAsync(today, cancellationToken);

        var moved = overdue.Count(task => task.RollOver(today));
        if (moved > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return moved;
    }
}
