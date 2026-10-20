using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Transactions → DTOs with category names and local times: shared by the history and the day of the calendar.</summary>
internal static class TransactionMapping
{
    public static async Task<IReadOnlyList<TransactionDto>> ToDtosAsync(this IFinanceCatalogRepository catalog, long chatId,
        IEnumerable<FinanceTransaction> transactions, TimeProvider time, CancellationToken cancellationToken)
    {
        var categories = await catalog.GetAllCategoriesAsync(chatId, cancellationToken);
        var names = categories.ToDictionary(c => c.Id, c => c.Name);

        return transactions
            .Select(t => TransactionDto.From(
                t,
                t.CategoryId is { } categoryId ? names.GetValueOrDefault(categoryId, "—") : null,
                time.ToLocal(t.CreatedAt)))
            .ToList();
    }
}
