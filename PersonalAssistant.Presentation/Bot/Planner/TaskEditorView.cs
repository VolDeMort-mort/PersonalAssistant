using System.Text;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Domain.Entities.Planner;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>
/// Screens of the task editor. Everything but the title and a new topic's name is picked with buttons.
/// </summary>
public static class TaskEditorView
{
    private const int MonthsAhead = 12;

    // After "Сьогодні" and "Завтра", the rest of the week gets a button per day
    private const int WeekdayButtons = 5;

    /// <summary>Minutes offered after an hour: a task time is precise enough to a quarter of an hour.</summary>
    public static readonly int[] MinuteOptions = { 0, 15, 30, 45 };

    private static readonly int[] Intervals = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 21, 30 };
    private const int FirstReminderHours = 3;
    private const int LastReminderHours = 24;

    /// <param name="topics">Needed only on the topic step.</param>
    /// <param name="today">In the user's time zone: past days are not offered.</param>
    public static BotScreen Step(PlannerDialog dialog, IReadOnlyList<TopicDto>? topics, DateOnly today, string? error)
    {
        var body = new StringBuilder();
        if (dialog.IsNewTask && DraftSummary(dialog.Draft, today) is { } draft)
            body.Append(draft).Append("\n\n");
        body.Append(Prompt(dialog));

        var title = dialog.IsNewTask ? "➕ Нова задача" : $"✏️ {dialog.Draft.Title}";
        return new BotScreen(ScreenText.Compose(title, body.ToString(), error), Keyboard(dialog, topics, today));
    }

    /// <summary>What was chosen so far, e.g. "«Стоматолог» · 📅 завтра о 14:00 · 🔁 щомісяця 30-го".</summary>
    private static string? DraftSummary(TaskDraft draft, DateOnly today)
    {
        var parts = new List<string>();
        if (draft.Title is not null)
            parts.Add($"«{HtmlText.Encode(draft.Title)}»");
        if (draft.Date is { } date)
            parts.Add($"📅 {PlannerView.DayLabel(date, today)}{(draft.Time is { } time ? $" о {PlannerView.Time(time)}" : "")}");
        if (draft.Repeat is { } repeat)
            parts.Add($"🔁 {PlannerView.RepeatLabel(repeat, draft.Date)}");
        if (draft.RemindMinutesBefore is { } minutes)
            parts.Add($"🔔 {PlannerView.ReminderLabel(minutes)}");
        if (draft.TopicName is not null)
            parts.Add($"🗂 {HtmlText.Encode(draft.TopicName)}");

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string Prompt(PlannerDialog dialog) => dialog.Step switch
    {
        EditorStep.Title when dialog.IsNewTask => "Надішліть назву задачі",
        EditorStep.Title => "Надішліть нову назву",
        EditorStep.Date => "На коли?",
        EditorStep.Month => "Оберіть місяць",
        EditorStep.Day when dialog.Draft.Month is { } month => $"{DateText.MonthName(month.Month)} {month.Year}: якого числа?",
        EditorStep.Day => "Якого числа?",
        EditorStep.Hour => "О котрій? Без часу — задача на весь день",
        EditorStep.Minute when dialog.Draft.Hour is { } hour => $"{hour:00}:?? — оберіть хвилини",
        EditorStep.Minute => "Оберіть хвилини",
        EditorStep.Repeat => "Повторювати?",
        EditorStep.Weekdays => "Оберіть дні тижня й натисніть «Готово»",
        EditorStep.Interval => "Кожні скільки днів?",
        EditorStep.Reminder => "Коли нагадати?",
        EditorStep.ReminderHours => "За скільки годин нагадати?",
        EditorStep.Topic => "Тема? Оберіть або надішліть назву нової",
        _ => "Надішліть назву нової теми. Емодзі на початку — за бажанням, напр. <code>🏃 Спорт</code>"
    };

    private static InlineKeyboardMarkup Keyboard(PlannerDialog dialog, IReadOnlyList<TopicDto>? topics, DateOnly today)
    {
        var rows = new List<InlineKeyboardButton[]>();

        switch (dialog.Step)
        {
            case EditorStep.Date:
                rows.Add(new[]
                {
                    Button("Сьогодні", TaskEditorPayloads.Date(today)),
                    Button("Завтра", TaskEditorPayloads.Date(today.AddDays(1)))
                });
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(2, WeekdayButtons)
                        .Select(offset => today.AddDays(offset))
                        .Select(d => Button(
                            $"{ScreenText.Capitalize(DateText.Weekday(d.DayOfWeek))} {d.Day:00}.{d.Month:00}",
                            TaskEditorPayloads.Date(d))), perRow: 3));
                rows.Add(new[]
                {
                    Button("📆 Інша дата", TaskEditorPayloads.OtherDate),
                    Button("📥 Без дати", TaskEditorPayloads.NoDate)
                });
                break;

            case EditorStep.Month:
                rows.AddRange(PickerKeyboards.Months(today, MonthsAhead, TaskEditorPayloads.Month));
                rows.Add(new[] { Button("🔙 До дат", TaskEditorPayloads.BackToDates) });
                break;

            case EditorStep.Day when dialog.Draft.Month is { } month:
                rows.AddRange(PickerKeyboards.Days(month, today, TaskEditorPayloads.Day));
                rows.Add(new[] { Button("🔙 До дат", TaskEditorPayloads.BackToDates) });
                break;

            case EditorStep.Hour:
                rows.Add(new[] { Button("🌤 Без часу", TaskEditorPayloads.NoTime) });
                rows.AddRange(PickerKeyboards.Hours(0, 23, TaskEditorPayloads.Hour, perRow: 6));
                break;

            case EditorStep.Minute when dialog.Draft.Hour is { } hour:
                rows.AddRange(KeyboardLayout.Grid(
                    MinuteOptions.Select(m => Button($"{hour:00}:{m:00}", TaskEditorPayloads.Minute(m))), perRow: 4));
                break;

            case EditorStep.Repeat:
                rows.Add(new[] { Button("Не повторювати", TaskEditorPayloads.Repeat(RepeatChoice.None)) });
                rows.Add(new[]
                {
                    Button("Щодня", TaskEditorPayloads.Repeat(RepeatChoice.Daily)),
                    Button("Дні тижня", TaskEditorPayloads.Repeat(RepeatChoice.Weekdays))
                });
                rows.Add(new[]
                {
                    Button("Кожні N днів", TaskEditorPayloads.Repeat(RepeatChoice.EveryNDays)),
                    Button("Щомісяця", TaskEditorPayloads.Repeat(RepeatChoice.Monthly))
                });
                rows.Add(new[] { Button("Щороку", TaskEditorPayloads.Repeat(RepeatChoice.Yearly)) });
                break;

            case EditorStep.Weekdays:
                rows.AddRange(KeyboardLayout.Grid(PickerKeyboards.WeekFromMonday.Select(d =>
                {
                    var name = ScreenText.Capitalize(DateText.Weekday(d));
                    return Button(dialog.Draft.Weekdays.Contains(d) ? $"✅ {name}" : name, TaskEditorPayloads.Weekday(d));
                }), perRow: 4));
                rows.Add(new[] { Button("✔️ Готово", TaskEditorPayloads.WeekdaysDone) });
                break;

            case EditorStep.Interval:
                rows.AddRange(KeyboardLayout.Grid(
                    Intervals.Select(n => Button(n.ToString(), TaskEditorPayloads.Interval(n))), perRow: 5));
                break;

            case EditorStep.Reminder:
                rows.Add(new[] { Button("У момент задачі", TaskEditorPayloads.Reminder(0)) });
                rows.Add(new[] { Button("За 15 хв", TaskEditorPayloads.Reminder(15)), Button("За 30 хв", TaskEditorPayloads.Reminder(30)) });
                rows.Add(new[] { Button("За 1 год", TaskEditorPayloads.Reminder(60)), Button("За 2 год", TaskEditorPayloads.Reminder(120)) });
                rows.Add(new[] { Button("⏳ Інше: за N год", TaskEditorPayloads.OtherReminder) });
                rows.Add(new[] { Button("🔕 Без нагадування", TaskEditorPayloads.NoReminder) });
                break;

            case EditorStep.ReminderHours:
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(FirstReminderHours, LastReminderHours - FirstReminderHours + 1)
                        .Select(h => Button($"{h} год", TaskEditorPayloads.ReminderHours(h))), perRow: 5));
                break;

            case EditorStep.Topic:
                rows.AddRange(KeyboardLayout.Pack((topics ?? Array.Empty<TopicDto>())
                    .Select(t => Button(t.Name, TaskEditorPayloads.Topic(t.Id)))));
                rows.Add(new[]
                {
                    Button("➕ Нова тема", TaskEditorPayloads.NewTopic),
                    Button("Без теми", TaskEditorPayloads.NoTopic)
                });
                break;

            case EditorStep.NewTopicName:
                rows.Add(new[] { Button("🔙 До тем", TaskEditorPayloads.BackToTopics) });
                break;
        }

        rows.Add(new[] { Button("✖️ Скасувати", TaskEditorPayloads.Cancel) });
        return new InlineKeyboardMarkup(rows);
    }

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
