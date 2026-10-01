using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Commands;

/// <param name="Hour">Null turns the morning list off.</param>
public record SetAgendaHourCommand(long ChatId, int? Hour) : IRequest;

public class SetAgendaHourCommandHandler : IRequestHandler<SetAgendaHourCommand>
{
    private readonly IPlannerSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;

    public SetAgendaHourCommandHandler(IPlannerSettingsRepository settings, IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetAgendaHourCommand request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(request.ChatId, cancellationToken);
        if (settings is null)
        {
            settings = PlannerSettings.Create(request.ChatId);
            _settings.Add(settings);
        }

        settings.SetAgendaHour(request.Hour);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
