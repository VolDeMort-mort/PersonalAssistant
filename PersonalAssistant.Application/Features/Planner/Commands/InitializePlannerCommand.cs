using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <summary>
/// Prepares the planner on the first visit: default topics and settings with the morning list at 08:00.
/// Returns true only on that first visit.
/// </summary>
public record InitializePlannerCommand(long ChatId) : IRequest<bool>;

public class InitializePlannerCommandHandler : IRequestHandler<InitializePlannerCommand, bool>
{
    private static readonly string[] DefaultTopics = { "💼 Робота", "🏠 Дім", "📚 Навчання", "🙂 Особисте" };

    private readonly IPlannerSettingsRepository _settings;
    private readonly IPlannerTopicRepository _topics;
    private readonly IUnitOfWork _unitOfWork;

    public InitializePlannerCommandHandler(IPlannerSettingsRepository settings, IPlannerTopicRepository topics, IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _topics = topics;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(InitializePlannerCommand request, CancellationToken cancellationToken)
    {
        if (await _settings.GetAsync(request.ChatId, cancellationToken) is not null)
            return false;

        _settings.Add(PlannerSettings.Create(request.ChatId));
        for (var i = 0; i < DefaultTopics.Length; i++)
            _topics.Add(PlannerTopic.Create(request.ChatId, DefaultTopics[i], sortOrder: i));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
