using PersonalAssistant.Application.Features.Finance.Dtos;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Application.Features.Finance.Queries;

/// <summary>Payments → DTOs with category names and days left: shared by the payment queries.</summary>
internal static class ScheduledPaymentMapping
{
    public static async Task<IReadOnlyList<ScheduledPaymentDto>> ToDtosAsync(this IFinanceCatalogRepository catalog,
        long chatId, IEnumerable<ScheduledPayment> payments, DateOnly today, CancellationToken cancellationToken)
    {
        var categories = await catalog.GetCategoriesAsync(chatId, TransactionType.Expense, cancellationToken);
        var names = categories.ToDictionary(c => c.Id, c => c.Name);

        return payments
            .Select(p => ScheduledPaymentDto.From(p, names.GetValueOrDefault(p.CategoryId, "—"), today))
            .ToList();
    }
}
