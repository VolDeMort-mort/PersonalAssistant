using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Queries;

/// <summary>Tasks of one day; today's list includes what is overdue.</summary>
public record GetDayQuery(long ChatId, DateOnly Date) : IRequest<DayPlanDto>;

public class GetDayQueryHandler : IRequestHandler<GetDayQuery, DayPlanDto>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;
    private readonly TimeProvider _time;

    public GetDayQueryHandler(IPlannerTaskRepository tasks, IPlannerTopicRepository topics, TimeProvider time)
    {
        _tasks = tasks;
        _topics = topics;
        _time = time;
    }

    public async Task<DayPlanDto> Handle(GetDayQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _tasks.GetOpenDueUntilAsync(request.ChatId, request.Date, cancellationToken);
        var topicNames = await _topics.GetTopicNamesAsync(request.ChatId, cancellationToken);

        var dayTasks = tasks.TasksOn(request.Date, _time.GetLocalToday(), topicNames).InDayOrder();
        return new DayPlanDto(request.Date, dayTasks);
    }
}
