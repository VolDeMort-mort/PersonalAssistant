using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <param name="CategoryId">An expense category; null takes the item out of its category.</param>
public record SetShoppingItemCategoryCommand(long ChatId, Guid ItemId, Guid? CategoryId) : IRequest;

public class SetShoppingItemCategoryCommandHandler : IRequestHandler<SetShoppingItemCategoryCommand>
{
    private readonly IShoppingListRepository _items;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;

    public SetShoppingItemCategoryCommandHandler(IShoppingListRepository items, IFinanceCatalogRepository catalog,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetShoppingItemCategoryCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetPendingItemAsync(request.ChatId, request.ItemId, cancellationToken);
        var category = await _catalog.GetCategoryOrNoneAsync(request.ChatId, request.CategoryId, cancellationToken);

        item.MoveToCategory(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
