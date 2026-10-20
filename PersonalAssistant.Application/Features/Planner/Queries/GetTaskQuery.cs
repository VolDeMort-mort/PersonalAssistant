using MediatR;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Queries;

/// <summary>The task card. Null when the task is gone or done: there is nothing left to show or change.</summary>
public record GetTaskQuery(long ChatId, Guid TaskId) : IRequest<TaskDetailsDto?>;

public class GetTaskQueryHandler : IRequestHandler<GetTaskQuery, TaskDetailsDto?>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;

    public GetTaskQueryHandler(IPlannerTaskRepository tasks, IPlannerTopicRepository topics)
    {
        _tasks = tasks;
        _topics = topics;
    }

    public async Task<TaskDetailsDto?> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(request.ChatId, request.TaskId, cancellationToken);
        if (task is null || task.IsDone)
            return null;

        var topicNames = await _topics.GetTopicNamesAsync(request.ChatId, cancellationToken);
        return TaskDetailsDto.From(task, topicNames.TopicNameOf(task));
    }
}
