using MediatR;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Commands;

/// <summary>
/// "✅ Куплено": records the expense and keeps the item as bought. Returns the transaction id.
/// </summary>
/// <param name="Amount">What was actually paid; null pays the item's price.</param>
/// <param name="CategoryId">Needed only when the item has no category yet.</param>
public record BuyShoppingItemCommand(long ChatId, Guid ItemId, long? Amount = null, Guid? CategoryId = null) : IRequest<Guid>;

public class BuyShoppingItemCommandHandler : IRequestHandler<BuyShoppingItemCommand, Guid>
{
    private readonly IShoppingListRepository _items;
    private readonly IFinanceCatalogRepository _catalog;
    private readonly IFinanceTransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public BuyShoppingItemCommandHandler(IShoppingListRepository items, IFinanceCatalogRepository catalog,
        IFinanceTransactionRepository transactions, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _items = items;
        _catalog = catalog;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<Guid> Handle(BuyShoppingItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetPendingItemAsync(request.ChatId, request.ItemId, cancellationToken);

        var categoryId = item.CategoryId ?? request.CategoryId
            ?? throw new InvalidOperationException("An item without a category needs one to be bought.");
        var category = await _catalog.GetCategoryAsync(request.ChatId, categoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceCategory), categoryId);

        var amount = request.Amount ?? item.Price
            ?? throw new InvalidOperationException("An item without a price needs the amount paid.");

        var transaction = item.Buy(category, amount, _time.GetUtcNow().UtcDateTime);
        _transactions.Add(transaction);

        // The expense and the bought item are saved together or not at all
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
