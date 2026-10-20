using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Queries;

/// <summary>Open tasks of a topic, nearest first, tasks without a date at the end.</summary>
public record GetTopicTasksQuery(long ChatId, Guid TopicId) : IRequest<TopicTasksDto>;

public class GetTopicTasksQueryHandler : IRequestHandler<GetTopicTasksQuery, TopicTasksDto>
{
    private readonly IPlannerTopicRepository _topics;
    private readonly IPlannerTaskRepository _tasks;

    public GetTopicTasksQueryHandler(IPlannerTopicRepository topics, IPlannerTaskRepository tasks)
    {
        _topics = topics;
        _tasks = tasks;
    }

    public async Task<TopicTasksDto> Handle(GetTopicTasksQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.ChatId, request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlannerTopic), request.TopicId);

        var tasks = await _tasks.GetOpenByTopicAsync(request.ChatId, topic.Id, cancellationToken);
        var items = tasks.Select(t => TaskDto.From(t, topic.Name)).InDateOrder();

        return new TopicTasksDto(new TopicDto(topic.Id, topic.Name, items.Count), items);
    }
}
