using System.Globalization;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <param name="Name">One of <see cref="PlannerPayloads.Actions"/>.</param>
/// <param name="Arg">Id, number, date or "id.date", depending on the action.</param>
public record PlannerAction(string Name, string? Arg) : CallbackAction(Name, Arg)
{
    /// <summary>A date written as yyyyMMdd.</summary>
    public DateOnly? Date => PlannerPayloads.ParseDate(Arg);

    /// <summary>"id.yyyyMMdd" of undo: the task and the date that was completed ("-" when it had none).</summary>
    public (Guid Id, DateOnly? Date)? Completion =>
        Arg?.Split('.') is [var id, var date] && Guid.TryParseExact(id, "N", out var guid)
            ? (guid, PlannerPayloads.ParseDate(date))
            : null;
}

/// <summary>Callback data of planner screens: "pln:action" or "pln:action:arg".</summary>
public static class PlannerPayloads
{
    public const string Prefix = "pln:";

    private const string DateFormat = "yyyyMMdd";
    private const string NoDate = "-";

    public static class Actions
    {
        public const string Home = "home";
        public const string Day = "day";
        public const string Week = "week";
        public const string Undated = "undated";
        public const string Topics = "topics";
        public const string Topic = "topic";
        public const string NewTopic = "newtopic";
        public const string DeleteTopic = "deltopic";
        public const string ConfirmDeleteTopic = "deltopicok";
        public const string Page = "page";
        public const string Card = "card";
        public const string Back = "back";
        public const string Done = "done";
        public const string Undo = "undo";
        public const string Delete = "del";
        public const string ConfirmDelete = "delok";
        public const string Settings = "settings";
        public const string AgendaHour = "agenda";
        public const string AgendaOff = "agendaoff";

        // Buttons of the reminder and the morning list, which are not the window
        public const string ReminderDone = "rdone";
        public const string ReminderIn15Minutes = "r15";
        public const string ReminderInHour = "r60";
        public const string ReminderTomorrow = "rtmr";
        public const string ReminderClose = "rclose";
        public const string OpenFromAgenda = "open";
    }

    public const string Home = Prefix + Actions.Home;
    public const string Week = Prefix + Actions.Week;
    public const string Undated = Prefix + Actions.Undated;
    public const string Topics = Prefix + Actions.Topics;
    public const string NewTopic = Prefix + Actions.NewTopic;
    public const string Back = Prefix + Actions.Back;
    public const string Settings = Prefix + Actions.Settings;
    public const string AgendaOff = Prefix + Actions.AgendaOff;
    public const string ReminderClose = Prefix + Actions.ReminderClose;
    public const string OpenFromAgenda = Prefix + Actions.OpenFromAgenda;

    public static string Day(DateOnly date) => Build(Actions.Day, FormatDate(date));
    public static string Topic(Guid id) => Build(Actions.Topic, id.ToString("N"));
    public static string DeleteTopic(Guid id) => Build(Actions.DeleteTopic, id.ToString("N"));
    public static string ConfirmDeleteTopic(Guid id) => Build(Actions.ConfirmDeleteTopic, id.ToString("N"));
    public static string Page(int page) => Build(Actions.Page, page.ToString(CultureInfo.InvariantCulture));
    public static string Card(Guid id) => Build(Actions.Card, id.ToString("N"));
    public static string Done(Guid id) => Build(Actions.Done, id.ToString("N"));
    public static string Undo(Guid id, DateOnly? completedDate) => Build(Actions.Undo, $"{id:N}.{FormatDate(completedDate)}");
    public static string Delete(Guid id) => Build(Actions.Delete, id.ToString("N"));
    public static string ConfirmDelete(Guid id) => Build(Actions.ConfirmDelete, id.ToString("N"));
    public static string AgendaHour(int hour) => Build(Actions.AgendaHour, hour.ToString(CultureInfo.InvariantCulture));
    public static string ReminderDone(Guid id) => Build(Actions.ReminderDone, id.ToString("N"));
    public static string ReminderIn15Minutes(Guid id) => Build(Actions.ReminderIn15Minutes, id.ToString("N"));
    public static string ReminderInHour(Guid id) => Build(Actions.ReminderInHour, id.ToString("N"));
    public static string ReminderTomorrow(Guid id) => Build(Actions.ReminderTomorrow, id.ToString("N"));

    public static PlannerAction Parse(string payload)
    {
        var (name, arg) = CallbackAction.Split(payload, Prefix);
        return new PlannerAction(name, arg);
    }

    internal static string FormatDate(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? NoDate;

    internal static DateOnly? ParseDate(string? text) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static string Build(string action, string arg) => $"{Prefix}{action}:{arg}";
}
