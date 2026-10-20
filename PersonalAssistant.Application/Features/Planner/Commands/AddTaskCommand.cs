using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// A new task. Quick add sends only the title and the list's date or topic; the wizard sends everything chosen.
/// Returns the new task id.
/// </summary>
/// <param name="RemindMinutesBefore">Needs <paramref name="DueTime"/>; 0 reminds at the time itself.</param>
public record AddTaskCommand(
    long ChatId,
    string Title,
    DateOnly? DueDate = null,
    TimeOnly? DueTime = null,
    RepeatInput? Repeat = null,
    int? RemindMinutesBefore = null,
    Guid? TopicId = null) : IRequest<Guid>;

public class AddTaskCommandHandler : IRequestHandler<AddTaskCommand, Guid>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public AddTaskCommandHandler(IPlannerTaskRepository tasks, IPlannerTopicRepository topics, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _tasks = tasks;
        _topics = topics;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(AddTaskCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetTopicOrNoneAsync(request.ChatId, request.TopicId, cancellationToken);
        var today = _time.GetLocalToday();

        // The same methods as editing the task later, so a new task obeys the same rules
        var task = PlannerTask.Create(request.ChatId, request.Title, request.DueDate, topic, _time.GetUtcNow().UtcDateTime);
        if (request.DueTime is { } time)
            task.SetTime(time);
        if (request.Repeat is { } repeat)
            task.SetRepeat(repeat.ToRule(task.DueDate ?? today), today);
        if (request.RemindMinutesBefore is { } minutes)
            task.SetReminder(minutes);

        _tasks.Add(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return task.Id;
    }
}
