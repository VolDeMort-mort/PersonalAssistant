using MediatR;
using PersonalAssistant.Application.Common;
using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Application.Features.Planner.Commands;
using PersonalAssistant.Application.Features.Planner.Dtos;
using PersonalAssistant.Application.Features.Planner.Queries;
using PersonalAssistant.Domain.Entities.Planner;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>
/// Planner scenarios as the user walks through them: the lists, the task card, quick add, settings
/// and the buttons of reminders. Handlers only translate buttons and messages into these calls.
/// </summary>
public class PlannerFlow
{
    private readonly ISender _sender;
    private readonly IBotMessenger _messenger;
    private readonly IDialogStore _dialogs;
    private readonly IScreenTracker _screens;
    private readonly TimeProvider _time;

    public PlannerFlow(ISender sender, IBotMessenger messenger, IDialogStore dialogs, IScreenTracker screens, TimeProvider time)
    {
        _sender = sender;
        _messenger = messenger;
        _dialogs = dialogs;
        _screens = screens;
        _time = time;
    }

    // ---------- Lists ----------

    /// <summary>Entry from the main menu: the first visit prepares topics and settings, then "Сьогодні".</summary>
    public async Task OpenAsync(long chatId, CancellationToken ct)
    {
        await _sender.Send(new InitializePlannerCommand(chatId), ct);
        await ShowHomeAsync(chatId, notice: null, ct);
    }

    public Task ShowHomeAsync(long chatId, string? notice, CancellationToken ct) =>
        ShowListAsync(chatId, PlannerList.Day(_time.GetLocalToday()), page: 0, notice, completed: null, ct);

    public Task ShowTomorrowAsync(long chatId, CancellationToken ct) =>
        ShowListAsync(chatId, PlannerList.Day(_time.GetLocalToday().AddDays(1)), page: 0, notice: null, completed: null, ct);

    /// <summary>A day picked in the week view; today is the main screen itself.</summary>
    public Task ShowWeekDayAsync(long chatId, DateOnly date, CancellationToken ct) =>
        date == _time.GetLocalToday()
            ? ShowHomeAsync(chatId, notice: null, ct)
            : ShowListAsync(chatId, PlannerList.Day(date, fromWeek: true), page: 0, notice: null, completed: null, ct);

    public Task ShowWeekAsync(long chatId, CancellationToken ct) =>
        ShowListAsync(chatId, PlannerList.Week, page: 0, notice: null, completed: null, ct);

    public Task ShowTopicsAsync(long chatId, string? notice, CancellationToken ct) =>
        ShowListAsync(chatId, PlannerList.Topics, page: 0, notice, completed: null, ct);

    public Task ShowTopicAsync(long chatId, Guid topicId, CancellationToken ct) =>
        ShowListAsync(chatId, PlannerList.Topic(topicId), page: 0, notice: null, completed: null, ct);

    public Task ChangePageAsync(long chatId, int page, CancellationToken ct) =>
        ShowListAsync(chatId, CurrentList(chatId), page, notice: null, completed: null, ct);

    /// <summary>
    /// The list the user was in: "🔙" from a card, and where to come back after an action.
    /// After a restart it is "Сьогодні".
    /// </summary>
    /// <param name="completed">A task just completed: the list offers to undo it.</param>
    public Task ShowCurrentListAsync(long chatId, string? notice, CancellationToken ct, CompletionDto? completed = null) =>
        ShowListAsync(chatId, CurrentList(chatId), CurrentPage(chatId), notice, completed, ct);

    // ---------- Task card ----------

    public async Task ShowCardAsync(long chatId, Guid taskId, string? notice, CancellationToken ct, bool confirmDelete = false)
    {
        var task = await GetTaskAsync(chatId, taskId, ct);
        var dialog = new PlannerDialog(CurrentList(chatId))
        {
            Page = CurrentPage(chatId),
            Screen = PlannerScreen.Card,
            TaskId = taskId
        };

        await RenderAsync(chatId, dialog, PlannerView.Card(task, _time.GetLocalToday(), notice, confirmDelete), ct);
    }

    public async Task CompleteAsync(long chatId, Guid taskId, CancellationToken ct)
    {
        var completion = await _sender.Send(new CompleteTaskCommand(chatId, taskId), ct);
        await ShowCurrentListAsync(chatId, PlannerView.DoneNotice(completion, _time.GetLocalToday()), ct, completion);
    }

    public async Task UndoAsync(long chatId, Guid taskId, DateOnly? completedDate, CancellationToken ct)
    {
        var reopened = await _sender.Send(new ReopenTaskCommand(chatId, taskId, completedDate), ct);
        await ShowCurrentListAsync(chatId, reopened ? PlannerView.ReopenedNotice : PlannerView.UndoFailedNotice, ct);
    }

    public Task AskDeleteAsync(long chatId, Guid taskId, CancellationToken ct) =>
        ShowCardAsync(chatId, taskId, notice: null, ct, confirmDelete: true);

    public async Task DeleteAsync(long chatId, Guid taskId, CancellationToken ct)
    {
        var task = await _sender.Send(new GetTaskQuery(chatId, taskId), ct);

        var notice = task is not null && await _sender.Send(new DeleteTaskCommand(chatId, taskId), ct)
            ? PlannerView.DeletedNotice(task.Title)
            : PlannerView.GoneNotice;

        await ShowCurrentListAsync(chatId, notice, ct);
    }

    // ---------- Topics ----------

    public async Task AskDeleteTopicAsync(long chatId, Guid topicId, CancellationToken ct)
    {
        var topic = await _sender.Send(new GetTopicTasksQuery(chatId, topicId), ct);
        var screen = PlannerView.Topic(topic, _time.GetLocalToday(), page: 0, notice: null, completed: null, confirmDelete: true);
        await RenderAsync(chatId, new PlannerDialog(PlannerList.Topic(topicId)), screen, ct);
    }

    public async Task DeleteTopicAsync(long chatId, Guid topicId, CancellationToken ct)
    {
        var topic = await _sender.Send(new GetTopicTasksQuery(chatId, topicId), ct);
        await _sender.Send(new DeleteTopicCommand(chatId, topicId), ct);
        await ShowTopicsAsync(chatId, PlannerView.TopicDeletedNotice(topic.Topic.Name), ct);
    }

    /// <summary>"➕ Нова тема": the name is typed next; "🔙" returns to the topics.</summary>
    public Task AskNewTopicAsync(long chatId, string? error, CancellationToken ct) =>
        RenderAsync(chatId, new PlannerDialog(PlannerList.Topics) { Screen = PlannerScreen.NewTopic }, PlannerView.NewTopic(error), ct);

    // ---------- Settings ----------

    public async Task ShowSettingsAsync(long chatId, string? notice, CancellationToken ct)
    {
        var settings = await _sender.Send(new GetPlannerSettingsQuery(chatId), ct);
        var dialog = new PlannerDialog(CurrentList(chatId)) { Page = CurrentPage(chatId), Screen = PlannerScreen.Settings };
        await RenderAsync(chatId, dialog, PlannerView.Settings(settings, notice), ct);
    }

    /// <param name="hour">Null turns the morning list off.</param>
    public async Task SetAgendaHourAsync(long chatId, int? hour, CancellationToken ct)
    {
        if (hour is < 0 or > 23)
        {
            await ShowSettingsAsync(chatId, notice: null, ct);
            return;
        }

        await _sender.Send(new SetAgendaHourCommand(chatId, hour), ct);
        await ShowSettingsAsync(chatId, PlannerView.SavedNotice, ct);
    }

    // ---------- Reminder and morning list messages ----------

    public async Task CompleteFromReminderAsync(long chatId, int reminderMessageId, Guid taskId, CancellationToken ct)
    {
        // The reminder goes first: after a restart it may have been taken as the window
        await _messenger.DeleteAsync(chatId, reminderMessageId, ct);

        var completion = await _sender.Send(new CompleteTaskCommand(chatId, taskId), ct);
        var today = _time.GetLocalToday();

        // A fresh window at the bottom of the chat, where the user is looking right now
        await ShowListAsync(chatId, PlannerList.Day(today), page: 0, PlannerView.DoneNotice(completion, today), completion, ct,
            newWindow: true);
    }

    public async Task SnoozeFromReminderAsync(long chatId, int reminderMessageId, Guid taskId, SnoozeOption option,
        CancellationToken ct)
    {
        await _messenger.DeleteAsync(chatId, reminderMessageId, ct);

        var remindAt = await _sender.Send(new SnoozeTaskCommand(chatId, taskId, option), ct);
        var today = _time.GetLocalToday();

        await ShowListAsync(chatId, PlannerList.Day(today), page: 0, PlannerView.SnoozedNotice(remindAt, today), completed: null, ct,
            newWindow: true);
    }

    public Task CloseReminderAsync(long chatId, int reminderMessageId, CancellationToken ct) =>
        _messenger.DeleteAsync(chatId, reminderMessageId, ct);

    /// <summary>"📅 Відкрити" under the morning list: the list stays, the window comes below it.</summary>
    public Task OpenFromAgendaAsync(long chatId, CancellationToken ct) =>
        ShowListAsync(chatId, PlannerList.Day(_time.GetLocalToday()), page: 0, notice: null, completed: null, ct, newWindow: true);

    // ---------- Typed text ----------

    public bool IsWaitingForText(long chatId) =>
        _dialogs.Get<PlannerDialog>(chatId) is { } dialog && dialog.ScreenId == _screens.Get(chatId);

    /// <summary>
    /// Text typed on a list: a new task of that list, or a new topic on the topic list
    /// and on the "➕ Нова тема" screen.
    /// </summary>
    public async Task HandleTextAsync(long chatId, string? text, CancellationToken ct)
    {
        var dialog = _dialogs.Get<PlannerDialog>(chatId);
        if (dialog is null)
            return;

        if (!dialog.IsOnList && !dialog.IsAskingTopicName)
        {
            await ShowAgainAsync(chatId, dialog, PlannerView.ChooseWithButtonsError, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            if (dialog.IsAskingTopicName)
                await AskNewTopicAsync(chatId, PlannerView.NotTextError, ct);
            else
                await ShowCurrentListAsync(chatId, PlannerView.NotTextError, ct);
            return;
        }

        text = text.Trim();
        switch (dialog.List)
        {
            case { Kind: PlannerListKind.Topics }:
                await AddTopicAsync(chatId, text, ct);
                break;

            case { TakesQuickAdd: true } list:
                await QuickAddAsync(chatId, list, text, ct);
                break;

            default:
                await ShowCurrentListAsync(chatId, PlannerView.WeekTextError, ct);
                break;
        }
    }

    private async Task QuickAddAsync(long chatId, PlannerList list, string title, CancellationToken ct)
    {
        if (title.Length > PlannerTask.MaxTitleLength)
        {
            await ShowCurrentListAsync(chatId, PlannerView.TooLongError(PlannerTask.MaxTitleLength), ct);
            return;
        }

        var taskId = await _sender.Send(new AddTaskCommand(chatId, title, DueDate: list.Date, TopicId: list.TopicId), ct);

        // The card right away: date, time, repeat and the rest are a tap away
        await ShowCardAsync(chatId, taskId, PlannerView.AddedNotice(title), ct);
    }

    private async Task AddTopicAsync(long chatId, string name, CancellationToken ct)
    {
        if (name.Length > PlannerTopic.MaxNameLength)
        {
            // The name prompt, whether it was typed there or straight on the topic list
            await AskNewTopicAsync(chatId, PlannerView.TooLongError(PlannerTopic.MaxNameLength), ct);
            return;
        }

        // May be an existing topic written differently, e.g. "дім" for "🏠 Дім": either way it opens, ready for tasks
        var topicId = await _sender.Send(new AddTopicCommand(chatId, name), ct);
        await ShowTopicAsync(chatId, topicId, ct);
    }

    /// <summary>Text on a card or the settings: the same screen again, with a hint to use the buttons.</summary>
    private Task ShowAgainAsync(long chatId, PlannerDialog dialog, string error, CancellationToken ct) => dialog switch
    {
        { Screen: PlannerScreen.Card, TaskId: { } taskId } => ShowCardAsync(chatId, taskId, error, ct),
        { Screen: PlannerScreen.Settings } => ShowSettingsAsync(chatId, error, ct),
        _ => ShowCurrentListAsync(chatId, error, ct)
    };

    // ---------- Shared with the editor ----------

    /// <summary>Draws a screen into the window and makes <paramref name="dialog"/> the chat's planner state.</summary>
    public async Task RenderAsync(long chatId, PlannerDialog dialog, BotScreen screen, CancellationToken ct, bool newWindow = false)
    {
        _dialogs.Set(chatId, dialog);

        if (newWindow)
            await _messenger.OpenScreenAsync(chatId, screen.Text, screen.Keyboard, ct);
        else
            await _messenger.ShowScreenAsync(chatId, screen.Text, screen.Keyboard, ct);

        // The window may have been re-sent as a new message: listen to the current one
        dialog.ScreenId = _screens.Get(chatId);
    }

    public PlannerList CurrentList(long chatId) =>
        _dialogs.Get<PlannerDialog>(chatId)?.List ?? PlannerList.Day(_time.GetLocalToday());

    public int CurrentPage(long chatId) => _dialogs.Get<PlannerDialog>(chatId)?.Page ?? 0;

    public async Task<TaskDetailsDto> GetTaskAsync(long chatId, Guid taskId, CancellationToken ct) =>
        await _sender.Send(new GetTaskQuery(chatId, taskId), ct)
        ?? throw new NotFoundException(nameof(PlannerTask), taskId);

    // ---------- Helpers ----------

    private async Task ShowListAsync(long chatId, PlannerList list, int page, string? notice, CompletionDto? completed,
        CancellationToken ct, bool newWindow = false)
    {
        var today = _time.GetLocalToday();

        var screen = list.Kind switch
        {
            PlannerListKind.Day => await DayScreenAsync(chatId, list, today, page, notice, completed, ct),
            PlannerListKind.Week =>
                PlannerView.Week(await _sender.Send(new GetWeekQuery(chatId), ct), today, notice),
            PlannerListKind.Topics =>
                PlannerView.Topics(await _sender.Send(new GetTopicsQuery(chatId), ct), notice),
            _ =>
                PlannerView.Topic(await _sender.Send(new GetTopicTasksQuery(chatId, list.TopicId!.Value), ct), today, page, notice,
                    completed, confirmDelete: false)
        };

        await RenderAsync(chatId, new PlannerDialog(list) { Page = page }, screen, ct, newWindow);
    }

    /// <summary>Today is the main screen, which also lists the tasks without a date; other days list only their own.</summary>
    private async Task<BotScreen> DayScreenAsync(long chatId, PlannerList list, DateOnly today, int page, string? notice,
        CompletionDto? completed, CancellationToken ct)
    {
        var day = await _sender.Send(new GetDayQuery(chatId, list.Date!.Value), ct);
        if (day.Date != today)
            return PlannerView.Day(day, today, list.FromWeek, page, notice, completed);

        var undated = await _sender.Send(new GetUndatedTasksQuery(chatId), ct);
        return PlannerView.Today(day, undated, page, notice, completed);
    }
}
