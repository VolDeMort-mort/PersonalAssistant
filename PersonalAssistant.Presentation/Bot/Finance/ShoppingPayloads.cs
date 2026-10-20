using System.Globalization;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <param name="Name">One of <see cref="ShoppingPayloads.Actions"/>.</param>
/// <param name="Arg">Id, page or "id.page", depending on the action.</param>
public record ShoppingAction(string Name, string? Arg) : CallbackAction(Name, Arg)
{
    /// <summary>"id.page" of a bought item's button: the item and the page of purchases to return to.</summary>
    public (Guid Id, int Page)? BoughtRef =>
        Arg?.Split('.') is [var id, var page] && Guid.TryParseExact(id, "N", out var guid)
            && int.TryParse(page, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? (guid, number)
            : null;
}

/// <summary>Callback data of shopping list buttons: "shp:action" or "shp:action:arg".</summary>
public static class ShoppingPayloads
{
    public const string Prefix = "shp:";

    public static class Actions
    {
        public const string List = "list";
        public const string Item = "item";
        public const string Delete = "del";
        public const string ConfirmDelete = "delok";

        // Buying: the category (when the item has none), then the amount
        public const string Buy = "buy";
        public const string BuyCategory = "buycat";
        public const string BuyExpected = "buyexp";
        public const string BuyOther = "buyother";
        public const string Undo = "undo";

        // The new item wizard and editing one field from the card share their steps
        public const string New = "new";
        public const string EditTitle = "etitle";
        public const string EditPrice = "eprice";
        public const string EditLink = "elink";
        public const string EditCategory = "ecat";
        public const string Skip = "skip";
        public const string Category = "cat";
        public const string NoCategory = "nocat";
        public const string NewCategory = "newcat";
        public const string BackToCategories = "cats";
        public const string Cancel = "cancel";

        public const string Bought = "bought";
        public const string BoughtItem = "bitem";
        public const string BuyAgain = "again";
    }

    public const string List = Prefix + Actions.List;
    public const string New = Prefix + Actions.New;
    public const string Skip = Prefix + Actions.Skip;
    public const string NoCategory = Prefix + Actions.NoCategory;
    public const string NewCategory = Prefix + Actions.NewCategory;
    public const string BackToCategories = Prefix + Actions.BackToCategories;
    public const string Cancel = Prefix + Actions.Cancel;

    public static string Item(Guid id) => Build(Actions.Item, Id(id));
    public static string Delete(Guid id) => Build(Actions.Delete, Id(id));
    public static string ConfirmDelete(Guid id) => Build(Actions.ConfirmDelete, Id(id));
    public static string Buy(Guid id) => Build(Actions.Buy, Id(id));
    public static string BuyCategory(Guid categoryId) => Build(Actions.BuyCategory, Id(categoryId));
    public static string BuyExpected(Guid id) => Build(Actions.BuyExpected, Id(id));
    public static string BuyOther(Guid id) => Build(Actions.BuyOther, Id(id));
    public static string Undo(Guid transactionId) => Build(Actions.Undo, Id(transactionId));
    public static string EditTitle(Guid id) => Build(Actions.EditTitle, Id(id));
    public static string EditPrice(Guid id) => Build(Actions.EditPrice, Id(id));
    public static string EditLink(Guid id) => Build(Actions.EditLink, Id(id));
    public static string EditCategory(Guid id) => Build(Actions.EditCategory, Id(id));
    public static string Category(Guid categoryId) => Build(Actions.Category, Id(categoryId));
    public static string Bought(int page) => Build(Actions.Bought, page.ToString(CultureInfo.InvariantCulture));
    public static string BoughtItem(Guid id, int page) => Build(Actions.BoughtItem, $"{Id(id)}.{page}");
    public static string BuyAgain(Guid id) => Build(Actions.BuyAgain, Id(id));

    public static ShoppingAction Parse(string payload)
    {
        var (name, arg) = CallbackAction.Split(payload, Prefix);
        return new ShoppingAction(name, arg);
    }

    private static string Id(Guid id) => id.ToString("N");

    private static string Build(string action, string arg) => $"{Prefix}{action}:{arg}";
}
