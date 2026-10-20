using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// Takes an item off the list without buying it. Returns false when it is already gone.
/// </summary>
public record DeleteShoppingItemCommand(long ChatId, Guid ItemId) : IRequest<bool>;

public class DeleteShoppingItemCommandHandler : IRequestHandler<DeleteShoppingItemCommand, bool>
{
    private readonly IShoppingListRepository _items;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteShoppingItemCommandHandler(IShoppingListRepository items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteShoppingItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ChatId, request.ItemId, cancellationToken);
        if (item is null)
            return false;

        _items.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
