using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

public record RenameShoppingItemCommand(long ChatId, Guid ItemId, string Title) : IRequest;

public class RenameShoppingItemCommandHandler : IRequestHandler<RenameShoppingItemCommand>
{
    private readonly IShoppingListRepository _items;
    private readonly IUnitOfWork _unitOfWork;

    public RenameShoppingItemCommandHandler(IShoppingListRepository items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RenameShoppingItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetPendingItemAsync(request.ChatId, request.ItemId, cancellationToken);
        item.Rename(request.Title);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
