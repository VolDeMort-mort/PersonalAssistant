using MediatR;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Queries;

/// <summary>Tasks for someday: the "📥 Без дати" part of the main screen.</summary>
public record GetUndatedTasksQuery(long ChatId) : IRequest<IReadOnlyList<TaskDto>>;

public class GetUndatedTasksQueryHandler : IRequestHandler<GetUndatedTasksQuery, IReadOnlyList<TaskDto>>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;

    public GetUndatedTasksQueryHandler(IPlannerTaskRepository tasks, IPlannerTopicRepository topics)
    {
        _tasks = tasks;
        _topics = topics;
    }

    public async Task<IReadOnlyList<TaskDto>> Handle(GetUndatedTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _tasks.GetOpenUndatedAsync(request.ChatId, cancellationToken);
        var topicNames = await _topics.GetTopicNamesAsync(request.ChatId, cancellationToken);

        return tasks.Select(t => TaskDto.From(t, topicNames.TopicNameOf(t))).InDateOrder();
    }
}
