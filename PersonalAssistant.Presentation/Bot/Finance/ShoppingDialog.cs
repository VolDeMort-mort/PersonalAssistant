namespace PersonalAssistant.Presentation.Bot.Finance;

public enum ShoppingStep
{
    // The new item wizard, and the same steps for editing one field from the card
    Title,
    Price,
    Link,
    Category,
    NewCategoryName,

    // Buying: the category of an item without one, then what was paid
    BuyCategory,
    BuyNewCategoryName,
    BuyConfirm,
    BuyAmount
}

/// <summary>The field of an item on the list being edited from its card.</summary>
public enum ShoppingField
{
    Title,
    Price,
    Link,
    Category
}

/// <summary>
/// The shopping list's state in a chat: the list itself, where typed text becomes new items,
/// a step of the new item wizard or of editing a field, or a step of buying.
/// </summary>
public class ShoppingDialog : BotDialog
{
    private ShoppingDialog(ShoppingStep? step) => Step = step;

    /// <summary>Null while the list is shown.</summary>
    public ShoppingStep? Step { get; set; }

    /// <summary>The item being edited or bought; null in the new item wizard.</summary>
    public Guid? ItemId { get; private init; }

    /// <summary>Null in the wizard and while buying.</summary>
    public ShoppingField? EditedField { get; private init; }

    // What the wizard collected so far
    public string? Title { get; set; }
    public long? Price { get; set; }
    public string? Link { get; set; }

    /// <summary>The item's own price, offered in one tap when buying.</summary>
    public long? ExpectedPrice { get; private init; }

    /// <summary>The category chosen while buying an item that had none.</summary>
    public Guid? BuyCategoryId { get; set; }

    public bool IsOnList => Step is null;
    public bool IsNewItem => Step is not null && ItemId is null;
    public bool IsBuying => Step is ShoppingStep.BuyCategory or ShoppingStep.BuyNewCategoryName or ShoppingStep.BuyConfirm
        or ShoppingStep.BuyAmount;

    public static ShoppingDialog OnList() => new(step: null);

    public static ShoppingDialog NewItem() => new(ShoppingStep.Title);

    public static ShoppingDialog Edit(Guid itemId, string title, ShoppingField field) => new(field switch
    {
        ShoppingField.Title => ShoppingStep.Title,
        ShoppingField.Price => ShoppingStep.Price,
        ShoppingField.Link => ShoppingStep.Link,
        _ => ShoppingStep.Category
    })
    {
        ItemId = itemId,
        Title = title,
        EditedField = field
    };

    /// <param name="hasCategory">An item without a category gets one chosen first.</param>
    /// <param name="price">The item's price; without one the amount is typed.</param>
    public static ShoppingDialog Buy(Guid itemId, string title, bool hasCategory, long? price) => new(
        !hasCategory ? ShoppingStep.BuyCategory : price is null ? ShoppingStep.BuyAmount : ShoppingStep.BuyConfirm)
    {
        ItemId = itemId,
        Title = title,
        ExpectedPrice = price
    };
}
