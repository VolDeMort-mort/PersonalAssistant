using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Finance;

/// <summary>
/// Something to buy, from the shopping list. Price, link and category are optional: "молоко" needs none of them.
/// Once bought, the item stays as a record of the purchase; buying it again is a new item.
/// </summary>
public class ShoppingItem
{
    public const int MaxTitleLength = 100;
    public const int MaxLinkLength = 500;

    private ShoppingItem() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public string Title { get; private set; } = null!;

    /// <summary>Expected price in whole hryvnias; null when not known. What was actually paid is in the expense.</summary>
    public long? Price { get; private set; }

    /// <summary>An absolute http(s) link to the product; null without one.</summary>
    public string? Link { get; private set; }

    /// <summary>An expense category; null until it is chosen, at the latest when the item is bought.</summary>
    public Guid? CategoryId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC; null while the item is still on the list.</summary>
    public DateTime? BoughtAt { get; private set; }

    public bool IsBought => BoughtAt is not null;

    /// <param name="price">Null when not known.</param>
    /// <param name="link">Null without one; otherwise an absolute http(s) link.</param>
    /// <param name="category">An expense category of the same chat; null for none.</param>
    public static ShoppingItem Create(long chatId, string title, long? price, string? link, FinanceCategory? category,
        DateTime createdAtUtc)
    {
        var item = new ShoppingItem
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Title = Guard.RequiredText(title, MaxTitleLength, nameof(title)),
            Price = ValidPrice(price),
            Link = ValidLink(link),
            CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
        };

        item.EnsureExpenseCategory(category);
        item.CategoryId = category?.Id;
        return item;
    }

    /// <summary>For checking typed text before it becomes a link: an absolute http(s) link of a sensible length.</summary>
    public static bool IsValidLink(string? link) =>
        link is { Length: > 0 and <= MaxLinkLength }
        && Uri.TryCreate(link, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https";

    public void Rename(string title)
    {
        EnsureOnList();
        Title = Guard.RequiredText(title, MaxTitleLength, nameof(title));
    }

    /// <param name="price">Null when not known.</param>
    public void SetPrice(long? price)
    {
        EnsureOnList();
        Price = ValidPrice(price);
    }

    /// <param name="link">Null removes it.</param>
    public void SetLink(string? link)
    {
        EnsureOnList();
        Link = ValidLink(link);
    }

    /// <param name="category">Null takes the item out of its category.</param>
    public void MoveToCategory(FinanceCategory? category)
    {
        EnsureOnList();
        EnsureExpenseCategory(category);
        CategoryId = category?.Id;
    }

    /// <summary>
    /// "✅ Куплено": records the expense with the amount actually paid, and the item becomes a record of the purchase.
    /// An item without a category is bought in <paramref name="category"/>; one with a category, only in its own.
    /// </summary>
    /// <param name="amount">What was actually paid; may differ from <see cref="Price"/>.</param>
    public FinanceTransaction Buy(FinanceCategory category, long amount, DateTime boughtAtUtc)
    {
        ArgumentNullException.ThrowIfNull(category);
        EnsureOnList();
        EnsureExpenseCategory(category);
        if (CategoryId is { } own && own != category.Id)
            throw new ArgumentException("The item belongs to another category.", nameof(category));

        var transaction = FinanceTransaction.CreateForShoppingItem(category, amount, Title, Id, boughtAtUtc);

        CategoryId = category.Id;
        BoughtAt = transaction.CreatedAt;
        return transaction;
    }

    /// <summary>Undo of <see cref="Buy"/>, when its expense is deleted: the item is back on the list.</summary>
    public void RevertPurchase() => BoughtAt = null;

    /// <summary>
    /// "🔁 Купити ще раз": a new item on the list with the same title, price, link and category.
    /// This one stays as it is, a record of the earlier purchase.
    /// </summary>
    public ShoppingItem CopyForNewPurchase(DateTime createdAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        ChatId = ChatId,
        Title = Title,
        Price = Price,
        Link = Link,
        CategoryId = CategoryId,
        CreatedAt = Guard.Utc(createdAtUtc, nameof(createdAtUtc))
    };

    private void EnsureOnList()
    {
        if (IsBought)
            throw new InvalidOperationException("This item is already bought.");
    }

    private void EnsureExpenseCategory(FinanceCategory? category)
    {
        if (category is null)
            return;
        if (category.ChatId != ChatId)
            throw new ArgumentException("The category belongs to another chat.", nameof(category));
        if (category.Type != TransactionType.Expense)
            throw new ArgumentException("A shopping item needs an expense category.", nameof(category));
    }

    private static long? ValidPrice(long? price) =>
        price is { } value ? Guard.Positive(value, nameof(price)) : null;

    private static string? ValidLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return null;

        var text = link.Trim();
        return IsValidLink(text)
            ? text
            : throw new ArgumentException($"Must be an http or https link of at most {MaxLinkLength} characters.", nameof(link));
    }
}
