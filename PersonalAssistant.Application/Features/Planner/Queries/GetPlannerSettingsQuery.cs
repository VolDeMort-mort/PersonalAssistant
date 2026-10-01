using MediatR;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Planner;

namespace PersonalAssistant.Application.Features.Planner.Queries;

public record GetPlannerSettingsQuery(long ChatId) : IRequest<PlannerSettingsDto>;

public class GetPlannerSettingsQueryHandler : IRequestHandler<GetPlannerSettingsQuery, PlannerSettingsDto>
{
    private readonly IPlannerSettingsRepository _settings;

    public GetPlannerSettingsQueryHandler(IPlannerSettingsRepository settings) => _settings = settings;

    public async Task<PlannerSettingsDto> Handle(GetPlannerSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(request.ChatId, cancellationToken);

        // Not created until the first visit; that visit will create them with the default hour
        return new PlannerSettingsDto(settings is null ? PlannerSettings.DefaultAgendaHour : settings.AgendaHour);
    }
}
