using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Finance;

/// <summary>
/// One movement of money. Amount is in whole hryvnias and always positive: Type says the direction.
/// The balance is never stored, it is the sum of all transactions.
/// </summary>
public class FinanceTransaction
{
    public const int MaxCommentLength = 200;

    private FinanceTransaction() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public TransactionType Type { get; private set; }
    public long Amount { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>
    /// Starting balance or a correction to match real money: counts in the balance,
    /// but not in monthly income and expenses. Has no category.
    /// </summary>
    public bool IsAdjustment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Chat and direction come from the category, so they can never disagree with it.
    /// </summary>
    public static FinanceTransaction Create(FinanceCategory category, long amount, string? comment, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new FinanceTransaction
        {
            Id = Guid.NewGuid(),
            ChatId = category.ChatId,
            Type = category.Type,
            Amount = Guard.Positive(amount, nameof(amount)),
            CategoryId = category.Id,
            Comment = Guard.OptionalText(comment, MaxCommentLength, nameof(comment)),
            CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
        };
    }

    /// <param name="balanceDifference">Real balance minus the balance the bot knows: positive adds money, negative removes it.</param>
    public static FinanceTransaction CreateAdjustment(long chatId, long balanceDifference, DateTime createdAtUtc)
    {
        if (balanceDifference == 0)
            throw new ArgumentException("An adjustment must change the balance.", nameof(balanceDifference));

        return new FinanceTransaction
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Type = balanceDifference > 0 ? TransactionType.Income : TransactionType.Expense,
            Amount = Math.Abs(balanceDifference),
            IsAdjustment = true,
            CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
        };
    }
}
