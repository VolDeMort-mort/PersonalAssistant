using PersonalAssistant.Domain.Entities.Finance;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <param name="Name">One of <see cref="PaymentPayloads.Actions"/>.</param>
/// <param name="Arg">Id, "id.day", number or recurrence code, depending on the action.</param>
public record PaymentAction(string Name, string? Arg) : CallbackAction(Name, Arg)
{
    public Recurrence? Recurrence => Arg switch
    {
        "o" => Domain.Entities.Finance.Recurrence.Once,
        "w" => Domain.Entities.Finance.Recurrence.Weekly,
        "m" => Domain.Entities.Finance.Recurrence.Monthly,
        "y" => Domain.Entities.Finance.Recurrence.Yearly,
        _ => null
    };

    /// <summary>
    /// "id" or "id.yyyyMMdd" of the card and paying buttons: the payment, and the calendar day it was opened from
    /// (null when opened from the list of planned expenses), so "🔙" and paying return there.
    /// </summary>
    public (Guid Id, DateOnly? FromDay)? PaymentRef => Arg?.Split('.') switch
    {
        [var id] when Guid.TryParseExact(id, "N", out var guid) => (guid, null),
        [var id, var day] when Guid.TryParseExact(id, "N", out var guid) && ReturnTo.TryParseDay(day, out var date) => (guid, date),
        _ => null
    };
}

/// <summary>Callback data of scheduled payment buttons: "sch:action" or "sch:action:arg".</summary>
public static class PaymentPayloads
{
    public const string Prefix = "sch:";

    public static class Actions
    {
        public const string List = "list";
        public const string Card = "card";
        public const string Paid = "paid";
        public const string PayExpected = "payexp";
        public const string PayOther = "payother";
        public const string Delete = "del";
        public const string New = "new";
        public const string Category = "cat";
        public const string NewCategory = "newcat";
        public const string BackToCategories = "cats";
        public const string Recurrence = "rec";
        public const string Weekday = "wd";
        public const string MonthDay = "md";
        public const string Month = "mon";
        public const string Day = "day";
        public const string RemindDays = "rd";
        public const string NoReminder = "norem";
        public const string RemindHour = "rh";
        public const string Cancel = "cancel";

        // Buttons of the reminder message, which is not the window
        public const string ReminderPaid = "rpaid";
        public const string ReminderClose = "rclose";
    }

    public const string List = Prefix + Actions.List;
    public const string New = Prefix + Actions.New;
    public const string NewCategory = Prefix + Actions.NewCategory;
    public const string BackToCategories = Prefix + Actions.BackToCategories;
    public const string NoReminder = Prefix + Actions.NoReminder;
    public const string Cancel = Prefix + Actions.Cancel;
    public const string ReminderClose = Prefix + Actions.ReminderClose;

    /// <param name="fromDay">The calendar day the card is opened from; null for the list of planned expenses.</param>
    public static string Card(Guid id, DateOnly? fromDay = null) => Build(Actions.Card, PaymentRef(id, fromDay));
    public static string Paid(Guid id, DateOnly? fromDay = null) => Build(Actions.Paid, PaymentRef(id, fromDay));
    public static string PayExpected(Guid id, DateOnly? fromDay = null) => Build(Actions.PayExpected, PaymentRef(id, fromDay));
    public static string PayOther(Guid id, DateOnly? fromDay = null) => Build(Actions.PayOther, PaymentRef(id, fromDay));
    public static string Delete(Guid id, DateOnly? fromDay = null) => Build(Actions.Delete, PaymentRef(id, fromDay));
    public static string Category(Guid id) => Build(Actions.Category, id.ToString("N"));
    public static string ReminderPaid(Guid id) => Build(Actions.ReminderPaid, id.ToString("N"));
    public static string Recurrence(Recurrence recurrence) => Build(Actions.Recurrence, Code(recurrence));
    public static string Weekday(DayOfWeek day) => Build(Actions.Weekday, ((int)day).ToString());
    public static string MonthDay(int day) => Build(Actions.MonthDay, day.ToString());
    public static string Month(DateOnly month) => Build(Actions.Month, $"{month.Year:0000}{month.Month:00}");
    public static string Day(int day) => Build(Actions.Day, day.ToString());
    public static string RemindDays(int days) => Build(Actions.RemindDays, days.ToString());
    public static string RemindHour(int hour) => Build(Actions.RemindHour, hour.ToString());

    public static PaymentAction Parse(string payload)
    {
        var (name, arg) = CallbackAction.Split(payload, Prefix);
        return new PaymentAction(name, arg);
    }

    private static string Build(string action, string arg) => $"{Prefix}{action}:{arg}";

    // "sch:payother:" + 32 + "." + 8 stays under Telegram's 64 bytes
    private static string PaymentRef(Guid id, DateOnly? fromDay) =>
        fromDay is { } day ? $"{id:N}.{ReturnTo.FormatDay(day)}" : id.ToString("N");

    private static string Code(Recurrence recurrence) => recurrence switch
    {
        Domain.Entities.Finance.Recurrence.Weekly => "w",
        Domain.Entities.Finance.Recurrence.Monthly => "m",
        Domain.Entities.Finance.Recurrence.Yearly => "y",
        _ => "o"
    };
}
