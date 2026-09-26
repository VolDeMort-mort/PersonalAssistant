using MediatR;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Adds a category to the end of the list. If one with this name already exists, returns its id
/// instead: typing an existing name simply picks that category.
/// </summary>
public record AddCategoryCommand(long ChatId, TransactionType Type, string Name) : IRequest<Guid>;

public class AddCategoryCommandHandler : IRequestHandler<AddCategoryCommand, Guid>
{
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;

    public AddCategoryCommandHandler(IFinanceCatalogRepository catalog, IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _catalog.GetCategoriesAsync(request.ChatId, request.Type, cancellationToken);

        var sameName = existing.FirstOrDefault(c =>
            string.Equals(c.Name, request.Name.Trim(), StringComparison.CurrentCultureIgnoreCase));
        if (sameName is not null)
            return sameName.Id;

        var sortOrder = existing.Count == 0 ? 0 : existing.Max(c => c.SortOrder) + 1;
        var category = FinanceCategory.Create(request.ChatId, request.Type, request.Name, sortOrder);
        _catalog.AddCategory(category);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
