using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <param name="Name">One of <see cref="FinancePayloads.Actions"/>.</param>
/// <param name="Arg">Id, transaction type, month, day or return target, depending on the action.</param>
public record FinanceAction(string Name, string? Arg) : CallbackAction(Name, Arg)
{
    public TransactionType? Type => Arg switch
    {
        FinancePayloads.ExpenseCode => TransactionType.Expense,
        FinancePayloads.IncomeCode => TransactionType.Income,
        _ => null
    };

    /// <summary>A month written as yyyyMM, as its first day.</summary>
    public DateOnly? Month => ReturnTo.TryParseMonth(Arg, out var month) ? month : null;

    /// <summary>A day written as yyyyMMdd.</summary>
    public DateOnly? Date => ReturnTo.TryParseDay(Arg, out var day) ? day : null;

    /// <summary>A page of a month's operations.</summary>
    public ReturnTo.MonthPage? MonthPage => ReturnTo.Parse(Arg) as ReturnTo.MonthPage;

    /// <summary>
    /// "id.token" of transaction buttons: the transaction and where to return (see <see cref="ReturnTo"/>).
    /// A bare id has no place to return to: undo then goes to the main finance screen.
    /// </summary>
    public (Guid Id, ReturnTo? Back)? TransactionRef => Arg?.Split('.') switch
    {
        [var id] when Guid.TryParseExact(id, "N", out var guid) => (guid, null),
        [var id, var token] when Guid.TryParseExact(id, "N", out var guid) && ReturnTo.Parse(token) is { } back => (guid, back),
        _ => null
    };
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
        public const string Calendar = "cal";
        public const string Day = "day";
        public const string History = "hist";
        public const string Transaction = "tx";
        public const string DeleteTransaction = "deltx";
        public const string ConfirmDeleteTransaction = "deltxok";

        /// <summary>Calendar cells that are only there to be read: weekday names, empty days.</summary>
        public const string Noop = "noop";
    }

    public const string Home = Prefix + Actions.Home;
    public const string Balance = Prefix + Actions.Balance;
    public const string NewCategory = Prefix + Actions.NewCategory;
    public const string BackToCategories = Prefix + Actions.BackToCategories;
    public const string Skip = Prefix + Actions.Skip;
    public const string Cancel = Prefix + Actions.Cancel;
    public const string Noop = Prefix + Actions.Noop;

    public static string Templates(TransactionType type) => Build(Actions.Templates, Code(type));
    public static string Manual(TransactionType type) => Build(Actions.Manual, Code(type));
    public static string NewTemplate(TransactionType type) => Build(Actions.NewTemplate, Code(type));
    public static string Template(Guid id) => Build(Actions.Template, id.ToString("N"));
    public static string UseTemplate(Guid id) => Build(Actions.UseTemplate, id.ToString("N"));
    public static string DeleteTemplate(Guid id) => Build(Actions.DeleteTemplate, id.ToString("N"));
    public static string Category(Guid id) => Build(Actions.Category, id.ToString("N"));

    /// <param name="back">Null returns to the main finance screen.</param>
    public static string Undo(Guid id, ReturnTo? back = null) => Build(Actions.Undo, TransactionRef(id, back));

    public static string Calendar(DateOnly month) => Build(Actions.Calendar, ReturnTo.FormatMonth(month));
    public static string Day(DateOnly date) => Build(Actions.Day, ReturnTo.FormatDay(date));
    public static string History(DateOnly month, int page) => Build(Actions.History, new ReturnTo.MonthPage(month, page).Token);
    public static string Transaction(Guid id, ReturnTo back) => Build(Actions.Transaction, TransactionRef(id, back));
    public static string DeleteTransaction(Guid id, ReturnTo back) => Build(Actions.DeleteTransaction, TransactionRef(id, back));
    public static string ConfirmDeleteTransaction(Guid id, ReturnTo back) => Build(Actions.ConfirmDeleteTransaction, TransactionRef(id, back));

    public static FinanceAction Parse(string payload)
    {
        var (name, arg) = CallbackAction.Split(payload, Prefix);
        return new FinanceAction(name, arg);
    }

    private static string Build(string action, string arg) => $"{Prefix}{action}:{arg}";

    // "fin:deltxok:" + 32 + "." + "m202610p12" stays well under Telegram's 64 bytes
    private static string TransactionRef(Guid id, ReturnTo? back) => back is null ? id.ToString("N") : $"{id:N}.{back.Token}";

    private static string Code(TransactionType type) => type == TransactionType.Income ? IncomeCode : ExpenseCode;
}
