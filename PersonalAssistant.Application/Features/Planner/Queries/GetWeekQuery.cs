using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Queries;

/// <summary>
/// Seven days from today. A repeated task is stored with its next date only,
/// so its other days in the week are worked out here from its <see cref="Domain.Entities.Planner.RepeatRule"/>.
/// </summary>
public record GetWeekQuery(long ChatId) : IRequest<WeekPlanDto>;

public class GetWeekQueryHandler : IRequestHandler<GetWeekQuery, WeekPlanDto>
{
    public const int DaysInWeek = 7;

    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;
    private readonly TimeProvider _time;

    public GetWeekQueryHandler(IPlannerTaskRepository tasks, IPlannerTopicRepository topics, TimeProvider time)
    {
        _tasks = tasks;
        _topics = topics;
        _time = time;
    }

    public async Task<WeekPlanDto> Handle(GetWeekQuery request, CancellationToken cancellationToken)
    {
        var today = _time.GetLocalToday();
        var lastDay = today.AddDays(DaysInWeek - 1);

        var tasks = await _tasks.GetOpenDueUntilAsync(request.ChatId, lastDay, cancellationToken);
        var topicNames = await _topics.GetTopicNamesAsync(request.ChatId, cancellationToken);

        var days = Enumerable.Range(0, DaysInWeek)
            .Select(offset => today.AddDays(offset))
            .Select(date => new DayPlanDto(date, tasks.TasksOn(date, today, topicNames).InDayOrder()))
            .ToList();

        return new WeekPlanDto(days);
    }
}
