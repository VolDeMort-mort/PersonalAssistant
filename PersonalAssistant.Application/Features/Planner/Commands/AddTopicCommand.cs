using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// Adds a topic to the end of the list. If one with the same name already exists
/// (see <see cref="PlannerTopic.HasSameName"/>), returns its id instead: typing "дім" simply picks "🏠 Дім".
/// </summary>
public record AddTopicCommand(long ChatId, string Name) : IRequest<Guid>;

public class AddTopicCommandHandler : IRequestHandler<AddTopicCommand, Guid>
{
    private readonly IPlannerTopicRepository _topics;
    private readonly IUnitOfWork _unitOfWork;

    public AddTopicCommandHandler(IPlannerTopicRepository topics, IUnitOfWork unitOfWork)
    {
        _topics = topics;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddTopicCommand request, CancellationToken cancellationToken)
    {
        var existing = await _topics.GetAllAsync(request.ChatId, cancellationToken);

        var sameName = existing.FirstOrDefault(t => t.HasSameName(request.Name));
        if (sameName is not null)
            return sameName.Id;

        var sortOrder = existing.Count == 0 ? 0 : existing.Max(t => t.SortOrder) + 1;
        var topic = PlannerTopic.Create(request.ChatId, request.Name, sortOrder);
        _topics.Add(topic);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return topic.Id;
    }
}
