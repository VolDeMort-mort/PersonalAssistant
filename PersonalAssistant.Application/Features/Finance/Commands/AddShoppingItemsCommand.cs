using MediatR;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Puts items on the shopping list: one from the wizard, or several typed at once, line by line.
/// They are saved together. Returns the new ids in the same order.
/// </summary>
public record AddShoppingItemsCommand(long ChatId, IReadOnlyList<NewShoppingItem> Items) : IRequest<IReadOnlyList<Guid>>;

public class AddShoppingItemsCommandHandler : IRequestHandler<AddShoppingItemsCommand, IReadOnlyList<Guid>>
{
    private readonly IShoppingListRepository _items;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public AddShoppingItemsCommandHandler(IShoppingListRepository items, IFinanceCatalogRepository catalog, IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _items = items;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<IReadOnlyList<Guid>> Handle(AddShoppingItemsCommand request, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var ids = new List<Guid>();
        foreach (var newItem in request.Items)
        {
            var category = await _catalog.GetCategoryOrNoneAsync(request.ChatId, newItem.CategoryId, cancellationToken);
            var item = ShoppingItem.Create(request.ChatId, newItem.Title, newItem.Price, newItem.Link, category, now);

            _items.Add(item);
            ids.Add(item.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ids;
    }
}
