using System.Globalization;
using System.Text;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Domain.Entities.Planner;
using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>
/// Turns planner data into screens and messages. Texts are HTML,
/// so everything the user typed goes through <see cref="HtmlText.Encode"/>.
/// </summary>
public static class PlannerView
{
    public const int PageSize = 8;

    // Tasks listed under each day of the week view; the rest are behind the day's button
    private const int MaxTasksPerWeekDay = 4;
    private const int MaxAgendaTasks = 20;
    private const int FirstAgendaHour = 5;
    private const int LastAgendaHour = 12;

    private const WeekDays Workdays =
        WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday;
    private const WeekDays Weekend = WeekDays.Saturday | WeekDays.Sunday;
    private const WeekDays AllDays = Workdays | Weekend;

    public const string GoneNotice = "⚠️ Цього вже немає";
    public const string ReopenedNotice = "↩️ Задачу повернуто";
    public const string UndoFailedNotice = "ℹ️ Скасувати вже не можна";
    public const string SavedNotice = "✅ Збережено";
    public const string NotTextError = "⚠️ Надішліть текст";
    public const string ChooseWithButtonsError = "⚠️ Оберіть варіант кнопкою";
    public const string WeekTextError = "⚠️ Щоб додати задачу, спершу відкрийте день";
    public const string DateInPastError = "⚠️ Ця дата вже минула";
    public const string NoWeekdaysError = "⚠️ Оберіть хоча б один день";

    public static string TooLongError(int maxLength) => $"⚠️ Задовго: максимум {maxLength} символів";
    public static string AddedNotice(string title) => $"✅ Додано: «{HtmlText.Encode(title)}»";
    public static string DeletedNotice(string title) => $"🗑 Видалено: «{HtmlText.Encode(title)}»";
    public static string TopicDeletedNotice(string name) => $"🗑 Тему «{HtmlText.Encode(name)}» видалено";

    public static string DoneNotice(CompletionDto completion, DateOnly today) =>
        completion.NextDueDate is { } next
            ? $"✅ «{HtmlText.Encode(completion.Title)}» — наступного разу {DayLabel(next, today)}"
            : $"✅ Виконано: «{HtmlText.Encode(completion.Title)}»";

    /// <param name="remindAt">Local moment of the next reminder; null when the task was only moved to tomorrow.</param>
    public static string SnoozedNotice(DateTime? remindAt, DateOnly today) =>
        remindAt is { } at
            ? $"⏰ Нагадаю {DayLabel(DateOnly.FromDateTime(at), today)} о {Time(TimeOnly.FromDateTime(at))}"
            : "📅 Перенесено на завтра";

    // ---------- Lists ----------

    /// <param name="completed">A task just completed from this list: the screen offers to undo it.</param>
    public static BotScreen Day(DayPlanDto day, DateOnly today, int page, string? notice, CompletionDto? completed)
    {
        var paged = Paginate(day.Tasks, page);

        var body = new StringBuilder(day.Tasks.Count == 0
            ? day.Date == today ? "На сьогодні задач немає 🎉" : "На цей день задач немає"
            : TaskLines(paged.Items, today, withDate: false, withTopic: true));
        body.Append(paged.Footer);
        body.Append("\n\n<i>Надішліть текст — додам задачу на цей день</i>");

        var rows = ListRows(paged, completed);
        if (day.Date == today)
        {
            rows.Add(new[] { Button("➕ Задача", TaskEditorPayloads.New), Button("📆 Тиждень", PlannerPayloads.Week) });
            rows.Add(new[] { Button("📥 Без дати", PlannerPayloads.Undated), Button("🗂 Теми", PlannerPayloads.Topics) });
            rows.Add(new[] { Button("⚙️ Налаштування", PlannerPayloads.Settings), Button("🔙 Меню", BotConstants.Payloads.NavRoot) });
        }
        else
        {
            rows.Add(new[] { Button("➕ Задача", TaskEditorPayloads.New) });
            rows.Add(new[] { Button("🔙 Тиждень", PlannerPayloads.Week), Button("📅 Сьогодні", PlannerPayloads.Home) });
        }

        return new BotScreen(ScreenText.Compose(DayTitle(day.Date, today), body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen Week(WeekPlanDto week, DateOnly today, string? notice)
    {
        var body = new StringBuilder();
        foreach (var day in week.Days)
        {
            body.Append("<b>").Append(HtmlText.Encode(DayTitle(day.Date, today, withIcon: false))).Append("</b>\n");

            if (day.Tasks.Count == 0)
                body.Append("<i>вільно</i>\n");
            foreach (var task in day.Tasks.Take(MaxTasksPerWeekDay))
                body.Append(TaskLine(task, today, withDate: false, withTopic: false)).Append('\n');
            if (day.Tasks.Count > MaxTasksPerWeekDay)
                body.Append($"<i>… ще {day.Tasks.Count - MaxTasksPerWeekDay}</i>\n");

            body.Append('\n');
        }

        var rows = KeyboardLayout.Pack(week.Days.Select(d => Button(
            d.Tasks.Count == 0 ? DayButton(d.Date, today) : $"{DayButton(d.Date, today)} · {d.Tasks.Count}",
            PlannerPayloads.Day(d.Date)))).ToList();
        rows.Add(new[] { Button("🔙 Назад", PlannerPayloads.Home) });

        return new BotScreen(ScreenText.Compose("📆 Тиждень", body.ToString().TrimEnd(), notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen Undated(IReadOnlyList<TaskDto> tasks, DateOnly today, int page, string? notice, CompletionDto? completed)
    {
        var paged = Paginate(tasks, page);

        var body = new StringBuilder(tasks.Count == 0
            ? "Задач без дати немає"
            : TaskLines(paged.Items, today, withDate: false, withTopic: true));
        body.Append(paged.Footer);
        body.Append("\n\n<i>Надішліть текст — додам задачу без дати</i>");

        var rows = ListRows(paged, completed);
        rows.Add(new[] { Button("➕ Задача", TaskEditorPayloads.New), Button("🔙 Назад", PlannerPayloads.Home) });

        return new BotScreen(ScreenText.Compose("📥 Без дати", body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    public static BotScreen Topics(IReadOnlyList<TopicDto> topics, string? notice)
    {
        var body = new StringBuilder(topics.Count == 0
            ? "Тем поки немає"
            : string.Join('\n', topics.Select(t => $"{HtmlText.Encode(t.Name)} · {TasksCount(t.OpenTasks)}")));
        body.Append("\n\n<i>Надішліть назву — додам нову тему</i>");

        var rows = KeyboardLayout.Pack(topics.Select(t => Button(t.Name, PlannerPayloads.Topic(t.Id)))).ToList();
        rows.Add(new[] { Button("➕ Нова тема", PlannerPayloads.NewTopic), Button("🔙 Назад", PlannerPayloads.Home) });

        return new BotScreen(ScreenText.Compose("🗂 Теми", body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    /// <summary>"➕ Нова тема": asks for the name, the only thing about a topic that has to be typed.</summary>
    public static BotScreen NewTopic(string? error)
    {
        const string body = "Надішліть назву нової теми. Емодзі на початку — за бажанням, напр. <code>🏃 Спорт</code>";
        var keyboard = new InlineKeyboardMarkup(new[] { new[] { Button("🔙 Назад", PlannerPayloads.Topics) } });
        return new BotScreen(ScreenText.Compose("🗂 Нова тема", body, error), keyboard);
    }

    /// <param name="confirmDelete">Asks "delete the topic?" instead of listing its tasks.</param>
    public static BotScreen Topic(TopicTasksDto topic, DateOnly today, int page, string? notice, CompletionDto? completed,
        bool confirmDelete)
    {
        if (confirmDelete)
        {
            var question = topic.Tasks.Count == 0
                ? "Видалити тему?"
                : $"Видалити тему? Її задачі ({topic.Tasks.Count}) залишаться, але без теми.";
            var confirmKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Button("🗑 Так, видалити", PlannerPayloads.ConfirmDeleteTopic(topic.Topic.Id)),
                    Button("✖️ Ні", PlannerPayloads.Topic(topic.Topic.Id))
                }
            });
            return new BotScreen(ScreenText.Compose(topic.Topic.Name, question), confirmKeyboard);
        }

        var paged = Paginate(topic.Tasks, page);

        var body = new StringBuilder(topic.Tasks.Count == 0
            ? "У цій темі задач немає"
            : TaskLines(paged.Items, today, withDate: true, withTopic: false));
        body.Append(paged.Footer);
        body.Append("\n\n<i>Надішліть текст — додам задачу в цю тему (без дати)</i>");

        var rows = ListRows(paged, completed);
        rows.Add(new[] { Button("➕ Задача", TaskEditorPayloads.New), Button("🗑 Видалити тему", PlannerPayloads.DeleteTopic(topic.Topic.Id)) });
        rows.Add(new[] { Button("🔙 Теми", PlannerPayloads.Topics) });

        return new BotScreen(ScreenText.Compose(topic.Topic.Name, body.ToString(), notice), new InlineKeyboardMarkup(rows));
    }

    // ---------- Task card ----------

    /// <param name="confirmDelete">Asks "delete it?" instead of offering the actions.</param>
    public static BotScreen Card(TaskDetailsDto task, DateOnly today, string? notice, bool confirmDelete)
    {
        var lines = new List<string>
        {
            task.DueDate is { } date
                ? $"📅 {DayLabel(date, today)}{(task.DueTime is { } time ? $" о {Time(time)}" : ", на весь день")}"
                : "📥 Без дати",
            $"🔁 {(task.Repeat is { } repeat ? RepeatLabel(repeat) : "не повторюється")}"
        };
        if (task.DueTime is not null)
            lines.Add($"🔔 {ReminderLabel(task.RemindMinutesBefore)}");
        lines.Add($"🗂 {(task.TopicName is { } topic ? HtmlText.Encode(topic) : "без теми")}");

        if (confirmDelete)
        {
            var confirmKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Button("🗑 Так, видалити", PlannerPayloads.ConfirmDelete(task.Id)),
                    Button("✖️ Ні", PlannerPayloads.Card(task.Id))
                }
            });
            return new BotScreen(ScreenText.Compose(task.Title, string.Join('\n', lines) + "\n\nВидалити задачу?"), confirmKeyboard);
        }

        var rows = new List<InlineKeyboardButton[]> { new[] { Button("✅ Виконано", PlannerPayloads.Done(task.Id)) } };

        // Time needs a date and a reminder needs a time: their buttons appear only then
        var scheduleRow = new List<InlineKeyboardButton> { Button("📅 Дата", TaskEditorPayloads.Edit(TaskField.Date, task.Id)) };
        if (task.DueDate is not null)
            scheduleRow.Add(Button("⏰ Час", TaskEditorPayloads.Edit(TaskField.Time, task.Id)));
        rows.Add(scheduleRow.ToArray());

        var repeatRow = new List<InlineKeyboardButton> { Button("🔁 Повтор", TaskEditorPayloads.Edit(TaskField.Repeat, task.Id)) };
        if (task.DueTime is not null)
            repeatRow.Add(Button("🔔 Нагадування", TaskEditorPayloads.Edit(TaskField.Reminder, task.Id)));
        rows.Add(repeatRow.ToArray());

        rows.Add(new[]
        {
            Button("🗂 Тема", TaskEditorPayloads.Edit(TaskField.Topic, task.Id)),
            Button("✏️ Назва", TaskEditorPayloads.Edit(TaskField.Title, task.Id))
        });
        rows.Add(new[] { Button("🗑 Видалити", PlannerPayloads.Delete(task.Id)), Button("🔙 Назад", PlannerPayloads.Back) });

        return new BotScreen(ScreenText.Compose(task.Title, string.Join('\n', lines), notice), new InlineKeyboardMarkup(rows));
    }

    // ---------- Settings ----------

    public static BotScreen Settings(PlannerSettingsDto settings, string? notice)
    {
        var state = settings.AgendaHour is { } hour ? $"щодня о <b>{hour:00}:00</b>" : "<b>вимкнено</b>";
        var body = $"☀️ Ранковий список задач на сьогодні: {state}\n\nОберіть годину:";

        var rows = PickerKeyboards.Hours(FirstAgendaHour, LastAgendaHour, PlannerPayloads.AgendaHour).ToList();
        var lastRow = new List<InlineKeyboardButton>();
        if (settings.AgendaHour is not null)
            lastRow.Add(Button("🔕 Вимкнути", PlannerPayloads.AgendaOff));
        lastRow.Add(Button("🔙 Назад", PlannerPayloads.Home));
        rows.Add(lastRow.ToArray());

        return new BotScreen(ScreenText.Compose("⚙️ Налаштування", body, notice), new InlineKeyboardMarkup(rows));
    }

    // ---------- Messages that are not the window ----------

    /// <summary>A separate message: only a new message makes the phone notify.</summary>
    public static BotScreen Reminder(TaskReminderDto reminder, DateOnly today)
    {
        var body = new StringBuilder($"<b>{HtmlText.Encode(reminder.Title)}</b>");
        if (reminder.DueDate is { } date)
            body.Append('\n').Append(ScreenText.Capitalize(DayLabel(date, today)))
                .Append(reminder.DueTime is { } time ? $" о {Time(time)}" : "");

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Button("✅ Готово", PlannerPayloads.ReminderDone(reminder.TaskId)),
                Button("🙈 Закрити", PlannerPayloads.ReminderClose)
            },
            new[]
            {
                Button("+15 хв", PlannerPayloads.ReminderIn15Minutes(reminder.TaskId)),
                Button("+1 год", PlannerPayloads.ReminderInHour(reminder.TaskId)),
                Button("Завтра", PlannerPayloads.ReminderTomorrow(reminder.TaskId))
            }
        });

        return new BotScreen(ScreenText.Compose("🔔 Нагадування", body.ToString()), keyboard);
    }

    /// <summary>The morning list: a separate message, like a reminder.</summary>
    public static BotScreen Agenda(DailyAgendaDto agenda)
    {
        var body = new StringBuilder(TaskLines(agenda.Tasks.Take(MaxAgendaTasks), agenda.Date, withDate: false, withTopic: true));
        if (agenda.Tasks.Count > MaxAgendaTasks)
            body.Append($"\n<i>… ще {agenda.Tasks.Count - MaxAgendaTasks}</i>");

        var keyboard = new InlineKeyboardMarkup(new[] { new[] { Button("📅 Відкрити планувальник", PlannerPayloads.OpenFromAgenda) } });
        var title = $"☀️ Сьогодні, {DateText.Short(agenda.Date)} · {TasksCount(agenda.Tasks.Count)}";
        return new BotScreen(ScreenText.Compose(title, body.ToString()), keyboard);
    }

    // ---------- Labels shared with the editor ----------

    /// <summary>"сьогодні", "завтра", "вчора" or "ср, 01.10".</summary>
    public static string DayLabel(DateOnly date, DateOnly today) => (date.DayNumber - today.DayNumber) switch
    {
        0 => "сьогодні",
        1 => "завтра",
        -1 => "вчора",
        _ => DateText.Short(date)
    };

    public static string Time(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string RepeatLabel(RepeatDto repeat) => repeat.Kind switch
    {
        RepeatKind.Daily => "щодня",
        RepeatKind.EveryNDays => $"кожні {DateText.Days(repeat.Interval)}",
        RepeatKind.Weekly => WeekdaysLabel(repeat.Days),
        RepeatKind.Monthly => $"щомісяця {repeat.AnchorDay}-го",
        _ => $"щороку {repeat.AnchorDay:00}.{repeat.AnchorMonth:00}"
    };

    /// <summary>A repeat chosen in the wizard: monthly and yearly ones get their day from <paramref name="date"/>.</summary>
    public static string RepeatLabel(RepeatInput repeat, DateOnly? date) => repeat.Kind switch
    {
        RepeatKind.Daily => "щодня",
        RepeatKind.EveryNDays => $"кожні {DateText.Days(repeat.Interval)}",
        RepeatKind.Weekly => WeekdaysLabel(repeat.Days),
        RepeatKind.Monthly => date is { } d ? $"щомісяця {d.Day}-го" : "щомісяця",
        _ => date is { } d ? $"щороку {d.Day:00}.{d.Month:00}" : "щороку"
    };

    public static string WeekdaysLabel(WeekDays days) => days switch
    {
        AllDays => "щодня",
        Workdays => "по буднях",
        Weekend => "по вихідних",
        _ => string.Join(", ", PickerKeyboards.WeekFromMonday.Where(d => days.Contains(d)).Select(DateText.Weekday))
    };

    /// <param name="minutesBefore">Null when there is no reminder.</param>
    public static string ReminderLabel(int? minutesBefore) => minutesBefore switch
    {
        null => "без нагадування",
        0 => "нагадати в момент задачі",
        < 60 => $"нагадати за {minutesBefore} хв",
        _ when minutesBefore % 60 == 0 => $"нагадати за {minutesBefore / 60} год",
        _ => $"нагадати за {minutesBefore / 60} год {minutesBefore % 60} хв"
    };

    // ---------- Helpers ----------

    /// <summary>A page of a list: its tasks, the "page 2 of 3" line and the arrows.</summary>
    private record Paged(IReadOnlyList<TaskDto> Items, int Page, int PageCount)
    {
        public string Footer => PageCount > 1 ? $"\n\n<i>Сторінка {Page + 1} з {PageCount}</i>" : "";
    }

    private static Paged Paginate(IReadOnlyList<TaskDto> tasks, int page)
    {
        var pageCount = Math.Max(1, (tasks.Count + PageSize - 1) / PageSize);
        page = Math.Clamp(page, 0, pageCount - 1);
        return new Paged(tasks.Skip(page * PageSize).Take(PageSize).ToList(), page, pageCount);
    }

    /// <summary>A button per task of the page, the arrows and the undo of a just completed task.</summary>
    private static List<InlineKeyboardButton[]> ListRows(Paged paged, CompletionDto? completed)
    {
        var rows = KeyboardLayout.Pack(paged.Items.Select(t => Button(TaskButton(t), PlannerPayloads.Card(t.Id)))).ToList();

        var arrows = new List<InlineKeyboardButton>();
        if (paged.Page > 0)
            arrows.Add(Button("◀️", PlannerPayloads.Page(paged.Page - 1)));
        if (paged.Page < paged.PageCount - 1)
            arrows.Add(Button("▶️", PlannerPayloads.Page(paged.Page + 1)));
        if (arrows.Count > 0)
            rows.Add(arrows.ToArray());

        if (completed is not null)
            rows.Add(new[] { Button("↩️ Скасувати", PlannerPayloads.Undo(completed.TaskId, completed.CompletedDate)) });

        return rows;
    }

    private static string TaskLines(IEnumerable<TaskDto> tasks, DateOnly today, bool withDate, bool withTopic) =>
        string.Join('\n', tasks.Select(t => TaskLine(t, today, withDate, withTopic)));

    /// <summary>"09:30 Подзвонити в банк 🔁 🔔 · 💼 Робота".</summary>
    private static string TaskLine(TaskDto task, DateOnly today, bool withDate, bool withTopic)
    {
        var line = new StringBuilder();
        if (task.DueDate is { } overdue && overdue < today)
            line.Append("⚠️ ");
        if (withDate && task.DueDate is { } date)
            line.Append(DateText.Short(date)).Append(' ');

        line.Append(task.DueTime is { } time ? $"<code>{Time(time)}</code>" : "▫️");
        line.Append(' ').Append(HtmlText.Encode(task.Title));

        if (task.IsRepeated)
            line.Append(" 🔁");
        if (task.HasReminder)
            line.Append(" 🔔");
        if (withTopic && task.TopicName is { } topic)
            line.Append(" · <i>").Append(HtmlText.Encode(topic)).Append("</i>");

        return line.ToString();
    }

    private static string TaskButton(TaskDto task) =>
        task.DueTime is { } time ? $"{Time(time)} {task.Title}" : task.Title;

    /// <summary>"📅 Сьогодні · пн, 29.09", "📅 Ср, 01.10".</summary>
    private static string DayTitle(DateOnly date, DateOnly today, bool withIcon = true)
    {
        var label = ScreenText.Capitalize(DayLabel(date, today));
        var title = Math.Abs(date.DayNumber - today.DayNumber) <= 1 ? $"{label} · {DateText.Short(date)}" : label;
        return withIcon ? $"📅 {title}" : title;
    }

    /// <summary>A day of the week view on a button: "Сьогодні", "Завтра", "Ср 01.10".</summary>
    private static string DayButton(DateOnly date, DateOnly today) => (date.DayNumber - today.DayNumber) switch
    {
        0 => "Сьогодні",
        1 => "Завтра",
        _ => $"{ScreenText.Capitalize(DateText.Weekday(date.DayOfWeek))} {date.ToString("dd.MM", CultureInfo.InvariantCulture)}"
    };

    private static string TasksCount(int count) => $"{count} {DateText.Plural(count, "задача", "задачі", "задач")}";

    private static InlineKeyboardButton Button(string text, string payload) =>
        InlineKeyboardButton.WithCallbackData(text, payload);
}
