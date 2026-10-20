using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IShoppingListRepository
{
    void Add(ShoppingItem item);
    void Remove(ShoppingItem item);

    /// <summary>Bought items too: buying again and undoing a purchase need them.</summary>
    Task<ShoppingItem?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken);

    /// <summary>Items still to buy.</summary>
    Task<IReadOnlyList<ShoppingItem>> GetPendingAsync(long chatId, CancellationToken cancellationToken);

    /// <summary>Bought items, the latest purchase first.</summary>
    Task<IReadOnlyList<ShoppingItem>> GetBoughtPageAsync(long chatId, int skip, int take, CancellationToken cancellationToken);

    Task<int> CountBoughtAsync(long chatId, CancellationToken cancellationToken);
}
