using MediatR;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <param name="Link">An absolute http(s) link; null removes it.</param>
public record SetShoppingItemLinkCommand(long ChatId, Guid ItemId, string? Link) : IRequest;

public class SetShoppingItemLinkCommandHandler : IRequestHandler<SetShoppingItemLinkCommand>
{
    private readonly IShoppingListRepository _items;
    private readonly IUnitOfWork _unitOfWork;

    public SetShoppingItemLinkCommandHandler(IShoppingListRepository items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetShoppingItemLinkCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetPendingItemAsync(request.ChatId, request.ItemId, cancellationToken);
        item.SetLink(request.Link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
