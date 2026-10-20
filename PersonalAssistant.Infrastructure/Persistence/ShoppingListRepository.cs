using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence;

public class ShoppingListRepository : IShoppingListRepository
{
    private readonly AppDbContext _context;

    public ShoppingListRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(ShoppingItem item) => _context.ShoppingItems.Add(item);
    public void Remove(ShoppingItem item) => _context.ShoppingItems.Remove(item);

    public Task<ShoppingItem?> GetByIdAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.ShoppingItems.FirstOrDefaultAsync(i => i.ChatId == chatId && i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ShoppingItem>> GetPendingAsync(long chatId, CancellationToken cancellationToken) =>
        await _context.ShoppingItems
            .AsNoTracking()
            .Where(i => i.ChatId == chatId && i.BoughtAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShoppingItem>> GetBoughtPageAsync(long chatId, int skip, int take,
        CancellationToken cancellationToken) =>
        await Bought(chatId)
            .AsNoTracking()
            .OrderByDescending(i => i.BoughtAt)
            .ThenBy(i => i.Id) // stable pages when several items were bought at once
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountBoughtAsync(long chatId, CancellationToken cancellationToken) =>
        Bought(chatId).CountAsync(cancellationToken);

    private IQueryable<ShoppingItem> Bought(long chatId) =>
        _context.ShoppingItems.Where(i => i.ChatId == chatId && i.BoughtAt != null);
}
