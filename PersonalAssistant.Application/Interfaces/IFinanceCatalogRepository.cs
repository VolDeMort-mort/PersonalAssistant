using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Interfaces;

/// <summary>
/// Reference data of the finance feature: categories and templates.
/// Changes are saved by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IFinanceCatalogRepository
{
    Task<bool> HasCategoriesAsync(long chatId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(long chatId, TransactionType type, CancellationToken cancellationToken);
    Task<FinanceCategory?> GetCategoryAsync(long chatId, Guid id, CancellationToken cancellationToken);

    /// <summary>Both income and expense categories, e.g. to name the transactions of a history page.</summary>
    Task<IReadOnlyList<FinanceCategory>> GetAllCategoriesAsync(long chatId, CancellationToken cancellationToken);
    void AddCategory(FinanceCategory category);

    Task<IReadOnlyList<FinanceTemplate>> GetTemplatesAsync(long chatId, TransactionType type, CancellationToken cancellationToken);
    Task<FinanceTemplate?> GetTemplateAsync(long chatId, Guid id, CancellationToken cancellationToken);
    void AddTemplate(FinanceTemplate template);
    void RemoveTemplate(FinanceTemplate template);
}
