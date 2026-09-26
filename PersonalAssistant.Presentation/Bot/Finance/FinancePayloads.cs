using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <param name="Name">One of <see cref="FinancePayloads.Actions"/>.</param>
/// <param name="Arg">Id or transaction type, depending on the action.</param>
public record FinanceAction(string Name, string? Arg) : CallbackAction(Name, Arg)
{
    public TransactionType? Type => Arg switch
    {
        FinancePayloads.ExpenseCode => TransactionType.Expense,
        FinancePayloads.IncomeCode => TransactionType.Income,
        _ => null
    };

    /// <summary>"id.page" of history buttons: the transaction and the history page to return to.</summary>
    public (Guid Id, int Page)? TransactionRef =>
        Arg?.Split('.') is [var id, var page] && Guid.TryParseExact(id, "N", out var guid) && int.TryParse(page, out var number)
            ? (guid, number)
            : null;
}

/// <summary>Callback data of finance buttons: "fin:action" or "fin:action:arg".</summary>
public static class FinancePayloads
{
    public const string Prefix = "fin:";

    public const string ExpenseCode = "e";
    public const string IncomeCode = "i";

    public static class Actions
    {
        public const string Home = "home";
        public const string Balance = "balance";
        public const string Templates = "tpls";
        public const string Template = "tpl";
        public const string UseTemplate = "use";
        public const string DeleteTemplate = "deltpl";
        public const string Manual = "manual";
        public const string NewTemplate = "newtpl";
        public const string Category = "cat";
        public const string NewCategory = "newcat";
        public const string BackToCategories = "cats";
        public const string Skip = "skip";
        public const string Cancel = "cancel";
        public const string Undo = "undo";
        public const string History = "hist";
        public const string Transaction = "tx";
        public const string DeleteTransaction = "deltx";
        public const string ConfirmDeleteTransaction = "deltxok";
    }

    public const string Home = Prefix + Actions.Home;
    public const string Balance = Prefix + Actions.Balance;
    public const string NewCategory = Prefix + Actions.NewCategory;
    public const string BackToCategories = Prefix + Actions.BackToCategories;
    public const string Skip = Prefix + Actions.Skip;
    public const string Cancel = Prefix + Actions.Cancel;

    public static string Templates(TransactionType type) => Build(Actions.Templates, Code(type));
    public static string Manual(TransactionType type) => Build(Actions.Manual, Code(type));
    public static string NewTemplate(TransactionType type) => Build(Actions.NewTemplate, Code(type));
    public static string Template(Guid id) => Build(Actions.Template, id.ToString("N"));
    public static string UseTemplate(Guid id) => Build(Actions.UseTemplate, id.ToString("N"));
    public static string DeleteTemplate(Guid id) => Build(Actions.DeleteTemplate, id.ToString("N"));
    public static string Category(Guid id) => Build(Actions.Category, id.ToString("N"));
    public static string Undo(Guid id) => Build(Actions.Undo, id.ToString("N"));
    public static string History(int page) => Build(Actions.History, page.ToString());
    public static string Transaction(Guid id, int page) => Build(Actions.Transaction, TransactionRef(id, page));
    public static string DeleteTransaction(Guid id, int page) => Build(Actions.DeleteTransaction, TransactionRef(id, page));
    public static string ConfirmDeleteTransaction(Guid id, int page) => Build(Actions.ConfirmDeleteTransaction, TransactionRef(id, page));

    public static FinanceAction Parse(string payload)
    {
        var (name, arg) = CallbackAction.Split(payload, Prefix);
        return new FinanceAction(name, arg);
    }

    private static string Build(string action, string arg) => $"{Prefix}{action}:{arg}";

    // "fin:deltxok:" + 32 + "." + page stays well under Telegram's 64 bytes
    private static string TransactionRef(Guid id, int page) => $"{id:N}.{page}";

    private static string Code(TransactionType type) => type == TransactionType.Income ? IncomeCode : ExpenseCode;
}
