using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Finance;

/// <summary>
/// A quick button for a repeating operation, e.g. "🚌 Проїзд · 30 ₴".
/// Without a fixed amount the bot asks for it every time the template is used.
/// </summary>
public class FinanceTemplate
{
    public const int MaxTitleLength = 30;

    private FinanceTemplate() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public TransactionType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public long? Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static FinanceTemplate Create(FinanceCategory category, string title, long? amount, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new FinanceTemplate
        {
            Id = Guid.NewGuid(),
            ChatId = category.ChatId,
            Type = category.Type,
            Title = Guard.RequiredText(title, MaxTitleLength, nameof(title)),
            CategoryId = category.Id,
            Amount = amount is null ? null : Guard.Positive(amount.Value, nameof(amount)),
            CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
        };
    }

    /// <param name="category">The template's own category.</param>
    /// <param name="amount">Overrides the template's amount; required when the template has none.</param>
    public FinanceTransaction CreateTransaction(FinanceCategory category, long? amount, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (category.Id != CategoryId)
            throw new ArgumentException("The category does not belong to this template.", nameof(category));

        var finalAmount = amount ?? Amount
            ?? throw new ArgumentException("This template has no fixed amount, so it must be given.", nameof(amount));

        return FinanceTransaction.Create(category, finalAmount, Title, createdAtUtc);
    }
}
