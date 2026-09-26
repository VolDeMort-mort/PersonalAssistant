using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

public record GetTemplatesQuery(long ChatId, TransactionType Type) : IRequest<IReadOnlyList<TemplateDto>>;

public class GetTemplatesQueryHandler : IRequestHandler<GetTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    private readonly IFinanceCatalogRepository _catalog;
    public GetTemplatesQueryHandler(IFinanceCatalogRepository catalog) => _catalog = catalog;

    public async Task<IReadOnlyList<TemplateDto>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _catalog.GetTemplatesAsync(request.ChatId, request.Type, cancellationToken);
        return templates.Select(t => new TemplateDto(t.Id, t.Title, t.Amount)).ToList();
    }
}
