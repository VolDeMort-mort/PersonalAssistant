using System.Text;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>
/// Screens of the task editor. The title, a new topic's name and the exact time are typed;
/// dates, repeats, reminders and topics are picked with buttons.
/// </summary>
public static class TaskEditorView
{
    private const int MonthsAhead = 12;

    // Under the big "Сьогодні": six days from tomorrow on, three in a row
    private const int NextDays = 6;

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
        EditorStep.Hour =>
            "⏰ О котрій? Надішліть годину від 0 до 23, напр. <code>9</code>\n«Без часу» — задача на весь день",
        // The hour is known, the minutes are still hidden under a spoiler until they are typed
        EditorStep.Minute when dialog.Draft.Hour is { } hour =>
            $"⏰ <b>{hour:00}:</b><tg-spoiler>00</tg-spoiler>\nТепер надішліть хвилини від 0 до 59, напр. <code>30</code>",
        EditorStep.Minute => "Надішліть хвилини від 0 до 59",
        EditorStep.Repeat => "Повторювати?",
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
                rows.Add(new[] { Button("Сьогодні", TaskEditorPayloads.Date(today)) });
                rows.AddRange(KeyboardLayout.Grid(
                    Enumerable.Range(1, NextDays)
                        .Select(offset => today.AddDays(offset))
                        .Select(d => Button(DateButton(d, today), TaskEditorPayloads.Date(d))), perRow: 3));
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

            case EditorStep.Repeat:
                rows.Add(new[]
                {
                    Button("Щодня", TaskEditorPayloads.Repeat(RepeatChoice.Daily)),
                    Button("Кожні N днів", TaskEditorPayloads.Repeat(RepeatChoice.EveryNDays))
                });
                rows.Add(new[]
                {
                    Button("Щомісяця", TaskEditorPayloads.Repeat(RepeatChoice.Monthly)),
                    Button("Щороку", TaskEditorPayloads.Repeat(RepeatChoice.Yearly))
                });
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

        // The step's "skip it" choice shares the last row with the way out
        var lastRow = new List<InlineKeyboardButton>();
        if (dialog.Step is EditorStep.Hour or EditorStep.Minute)
            lastRow.Add(Button("🌤 Без часу", TaskEditorPayloads.NoTime));
        if (dialog.Step == EditorStep.Repeat)
            lastRow.Add(Button("Не повторювати", TaskEditorPayloads.Repeat(RepeatChoice.None)));
        lastRow.Add(Button("✖️ Скасувати", TaskEditorPayloads.Cancel));
        rows.Add(lastRow.ToArray());

        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>"Завтра", then "Пн 06.10" and so on.</summary>
    private static string DateButton(DateOnly date, DateOnly today) =>
        date == today.AddDays(1)
            ? "Завтра"
            : $"{ScreenText.Capitalize(DateText.Weekday(date.DayOfWeek))} {date.Day:00}.{date.Month:00}";

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
