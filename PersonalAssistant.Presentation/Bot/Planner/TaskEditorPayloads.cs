using System.Globalization;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <param name="Name">One of <see cref="TaskEditorPayloads.Actions"/>.</param>
/// <param name="Arg">Id, number, date or repeat code, depending on the action.</param>
public record TaskEditorAction(string Name, string? Arg) : CallbackAction(Name, Arg)
{
    /// <summary>A date written as yyyyMMdd.</summary>
    public DateOnly? Date => PlannerPayloads.ParseDate(Arg);

    /// <summary>The repeat chosen on the repeat step; <see cref="RepeatChoice.None"/> for "Не повторювати".</summary>
    public RepeatChoice? Repeat => Arg switch
    {
        "n" => RepeatChoice.None,
        "d" => RepeatChoice.Daily,
        "w" => RepeatChoice.Weekdays,
        "i" => RepeatChoice.EveryNDays,
        "m" => RepeatChoice.Monthly,
        "y" => RepeatChoice.Yearly,
        _ => null
    };
}

/// <summary>Buttons of the repeat step; the weekday and interval ones lead to one more step.</summary>
public enum RepeatChoice
{
    None,
    Daily,
    Weekdays,
    EveryNDays,
    Monthly,
    Yearly
}

/// <summary>Callback data of the task editor: "tsk:action" or "tsk:action:arg".</summary>
public static class TaskEditorPayloads
{
    public const string Prefix = "tsk:";

    public static class Actions
    {
        public const string New = "new";
        public const string EditTitle = "etitle";
        public const string EditDate = "edate";
        public const string EditTime = "etime";
        public const string EditRepeat = "erep";
        public const string EditReminder = "erem";
        public const string EditTopic = "etopic";

        public const string Date = "date";
        public const string OtherDate = "other";
        public const string NoDate = "nodate";
        public const string Month = "mon";
        public const string Day = "day";
        public const string BackToDates = "dates";
        public const string Hour = "hour";
        public const string Minute = "min";
        public const string NoTime = "notime";
        public const string Repeat = "rep";
        public const string Weekday = "wd";
        public const string WeekdaysDone = "wdok";
        public const string Interval = "int";
        public const string Reminder = "rem";
        public const string OtherReminder = "remother";
        public const string ReminderHours = "remh";
        public const string NoReminder = "norem";
        public const string Topic = "topic";
        public const string NoTopic = "notopic";
        public const string NewTopic = "newtopic";
        public const string BackToTopics = "topics";
        public const string Cancel = "cancel";
    }

    public const string New = Prefix + Actions.New;
    public const string OtherDate = Prefix + Actions.OtherDate;
    public const string NoDate = Prefix + Actions.NoDate;
    public const string BackToDates = Prefix + Actions.BackToDates;
    public const string NoTime = Prefix + Actions.NoTime;
    public const string WeekdaysDone = Prefix + Actions.WeekdaysDone;
    public const string OtherReminder = Prefix + Actions.OtherReminder;
    public const string NoReminder = Prefix + Actions.NoReminder;
    public const string NoTopic = Prefix + Actions.NoTopic;
    public const string NewTopic = Prefix + Actions.NewTopic;
    public const string BackToTopics = Prefix + Actions.BackToTopics;
    public const string Cancel = Prefix + Actions.Cancel;

    public static string Edit(TaskField field, Guid taskId) => Build(field switch
    {
        TaskField.Title => Actions.EditTitle,
        TaskField.Date => Actions.EditDate,
        TaskField.Time => Actions.EditTime,
        TaskField.Repeat => Actions.EditRepeat,
        TaskField.Reminder => Actions.EditReminder,
        _ => Actions.EditTopic
    }, taskId.ToString("N"));

    public static string Date(DateOnly date) => Build(Actions.Date, PlannerPayloads.FormatDate(date));
    public static string Month(DateOnly month) => Build(Actions.Month, $"{month.Year:0000}{month.Month:00}");
    public static string Day(int day) => Build(Actions.Day, Number(day));
    public static string Hour(int hour) => Build(Actions.Hour, Number(hour));
    public static string Minute(int minute) => Build(Actions.Minute, Number(minute));
    public static string Weekday(DayOfWeek day) => Build(Actions.Weekday, Number((int)day));
    public static string Interval(int days) => Build(Actions.Interval, Number(days));
    public static string Reminder(int minutesBefore) => Build(Actions.Reminder, Number(minutesBefore));
    public static string ReminderHours(int hours) => Build(Actions.ReminderHours, Number(hours));
    public static string Topic(Guid id) => Build(Actions.Topic, id.ToString("N"));

    public static string Repeat(RepeatChoice choice) => Build(Actions.Repeat, choice switch
    {
        RepeatChoice.Daily => "d",
        RepeatChoice.Weekdays => "w",
        RepeatChoice.EveryNDays => "i",
        RepeatChoice.Monthly => "m",
        RepeatChoice.Yearly => "y",
        _ => "n"
    });

    public static TaskEditorAction Parse(string payload)
    {
        var (name, arg) = CallbackAction.Split(payload, Prefix);
        return new TaskEditorAction(name, arg);
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Build(string action, string arg) => $"{Prefix}{action}:{arg}";
}
