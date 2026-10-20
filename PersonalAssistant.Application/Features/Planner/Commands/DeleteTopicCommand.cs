using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// Deletes a topic; its tasks stay, without a topic. Returns false when the topic is already gone.
/// </summary>
public record DeleteTopicCommand(long ChatId, Guid TopicId) : IRequest<bool>;

public class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, bool>
{
    private readonly IPlannerTopicRepository _topics;
    private readonly IPlannerTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTopicCommandHandler(IPlannerTopicRepository topics, IPlannerTaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _topics = topics;
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.ChatId, request.TopicId, cancellationToken);
        if (topic is null)
            return false;

        // Said by the domain, not left to a cascade in the database
        foreach (var task in await _tasks.GetAllByTopicAsync(request.ChatId, topic.Id, cancellationToken))
            task.MoveToTopic(null);

        _topics.Remove(topic);

        // The tasks and the topic go together or not at all
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
