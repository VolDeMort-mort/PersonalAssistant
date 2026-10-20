using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <param name="TopicId">Null takes the task out of its topic.</param>
public record SetTaskTopicCommand(long ChatId, Guid TaskId, Guid? TopicId) : IRequest;

public class SetTaskTopicCommandHandler : IRequestHandler<SetTaskTopicCommand>
{
    private readonly IPlannerTaskRepository _tasks;
    private readonly IPlannerTopicRepository _topics;
    private readonly IUnitOfWork _unitOfWork;

    public SetTaskTopicCommandHandler(IPlannerTaskRepository tasks, IPlannerTopicRepository topics, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _topics = topics;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetTaskTopicCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetOpenTaskAsync(request.ChatId, request.TaskId, cancellationToken);
        var topic = await _topics.GetTopicOrNoneAsync(request.ChatId, request.TopicId, cancellationToken);

        task.MoveToTopic(topic);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
