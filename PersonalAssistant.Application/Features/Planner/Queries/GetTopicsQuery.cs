using MediatR;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Queries;

/// <summary>Topics in the user's order, with how many open tasks each has.</summary>
public record GetTopicsQuery(long ChatId) : IRequest<IReadOnlyList<TopicDto>>;

public class GetTopicsQueryHandler : IRequestHandler<GetTopicsQuery, IReadOnlyList<TopicDto>>
{
    private readonly IPlannerTopicRepository _topics;
    private readonly IPlannerTaskRepository _tasks;

    public GetTopicsQueryHandler(IPlannerTopicRepository topics, IPlannerTaskRepository tasks)
    {
        _topics = topics;
        _tasks = tasks;
    }

    public async Task<IReadOnlyList<TopicDto>> Handle(GetTopicsQuery request, CancellationToken cancellationToken)
    {
        var topics = await _topics.GetAllAsync(request.ChatId, cancellationToken);
        var openTasks = await _tasks.CountOpenByTopicAsync(request.ChatId, cancellationToken);

        return topics.Select(t => new TopicDto(t.Id, t.Name, openTasks.GetValueOrDefault(t.Id))).ToList();
    }
}
