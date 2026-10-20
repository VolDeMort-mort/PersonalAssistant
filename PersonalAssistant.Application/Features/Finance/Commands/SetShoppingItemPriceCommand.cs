using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <param name="Price">Whole hryvnias; null when not known.</param>
public record SetShoppingItemPriceCommand(long ChatId, Guid ItemId, long? Price) : IRequest;

public class SetShoppingItemPriceCommandHandler : IRequestHandler<SetShoppingItemPriceCommand>
{
    private readonly IShoppingListRepository _items;
    private readonly IUnitOfWork _unitOfWork;

    public SetShoppingItemPriceCommandHandler(IShoppingListRepository items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetShoppingItemPriceCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetPendingItemAsync(request.ChatId, request.ItemId, cancellationToken);
        item.SetPrice(request.Price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
