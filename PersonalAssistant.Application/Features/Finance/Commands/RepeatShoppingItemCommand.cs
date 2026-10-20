using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// "🔁 Купити ще раз": a bought item goes back on the list as a new one, its purchase stays in the history.
/// Returns the new item id.
/// </summary>
public record RepeatShoppingItemCommand(long ChatId, Guid ItemId) : IRequest<Guid>;

public class RepeatShoppingItemCommandHandler : IRequestHandler<RepeatShoppingItemCommand, Guid>
{
    private readonly IShoppingListRepository _items;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public RepeatShoppingItemCommandHandler(IShoppingListRepository items, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _items = items;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(RepeatShoppingItemCommand request, CancellationToken cancellationToken)
    {
        var bought = await _items.GetByIdAsync(request.ChatId, request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingItem), request.ItemId);

        var item = bought.CopyForNewPurchase(_time.GetUtcNow().UtcDateTime);
        _items.Add(item);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}
