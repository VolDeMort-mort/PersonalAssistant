using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Infrastructure.Persistence;

public class FinanceCatalogRepository : IFinanceCatalogRepository
{
    private readonly AppDbContext _context;

    public FinanceCatalogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> HasCategoriesAsync(long chatId, CancellationToken cancellationToken) =>
        _context.FinanceCategories.AnyAsync(c => c.ChatId == chatId, cancellationToken);

    public async Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(long chatId, TransactionType type, CancellationToken cancellationToken) =>
        await _context.FinanceCategories
            .AsNoTracking()
            .Where(c => c.ChatId == chatId && c.Type == type)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<FinanceCategory?> GetCategoryAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.FinanceCategories.FirstOrDefaultAsync(c => c.ChatId == chatId && c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FinanceCategory>> GetAllCategoriesAsync(long chatId, CancellationToken cancellationToken) =>
        await _context.FinanceCategories
            .AsNoTracking()
            .Where(c => c.ChatId == chatId)
            .ToListAsync(cancellationToken);

    public void AddCategory(FinanceCategory category) => _context.FinanceCategories.Add(category);

    public async Task<IReadOnlyList<FinanceTemplate>> GetTemplatesAsync(long chatId, TransactionType type, CancellationToken cancellationToken) =>
        await _context.FinanceTemplates
            .AsNoTracking()
            .Where(t => t.ChatId == chatId && t.Type == type)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<FinanceTemplate?> GetTemplateAsync(long chatId, Guid id, CancellationToken cancellationToken) =>
        _context.FinanceTemplates.FirstOrDefaultAsync(t => t.ChatId == chatId && t.Id == id, cancellationToken);

    public void AddTemplate(FinanceTemplate template) => _context.FinanceTemplates.Add(template);
    public void RemoveTemplate(FinanceTemplate template) => _context.FinanceTemplates.Remove(template);
}
