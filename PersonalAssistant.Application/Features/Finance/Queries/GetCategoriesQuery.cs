using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

public record GetCategoriesQuery(long ChatId, TransactionType Type) : IRequest<IReadOnlyList<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IFinanceCatalogRepository _catalog;
    public GetCategoriesQueryHandler(IFinanceCatalogRepository catalog) => _catalog = catalog;

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _catalog.GetCategoriesAsync(request.ChatId, request.Type, cancellationToken);
        return categories.Select(c => new CategoryDto(c.Id, c.Name)).ToList();
    }
}
