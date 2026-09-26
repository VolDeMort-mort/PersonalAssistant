using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Everything the template card shows. Null when the template is gone.</summary>
public record GetTemplateQuery(long ChatId, Guid TemplateId) : IRequest<TemplateDetailsDto?>;

public class GetTemplateQueryHandler : IRequestHandler<GetTemplateQuery, TemplateDetailsDto?>
{
    private readonly IFinanceCatalogRepository _catalog;
    public GetTemplateQueryHandler(IFinanceCatalogRepository catalog) => _catalog = catalog;

    public async Task<TemplateDetailsDto?> Handle(GetTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _catalog.GetTemplateAsync(request.ChatId, request.TemplateId, cancellationToken);
        if (template is null)
            return null;

        var category = await _catalog.GetCategoryAsync(request.ChatId, template.CategoryId, cancellationToken);

        return new TemplateDetailsDto(template.Id, template.Type, template.Title, category?.Name ?? "—", template.Amount);
    }
}
