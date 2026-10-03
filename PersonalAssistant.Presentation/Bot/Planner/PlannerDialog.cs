using PersonalAssistant.Application.Features.Planner.Dtos;

namespace PersonalAssistant.Presentation.Bot.Planner;

/// <summary>What the window shows while no editor step is active.</summary>
public enum PlannerScreen
{
    List,
    Card,
    Settings,

    /// <summary>"➕ Нова тема": waits for the topic's name.</summary>
    NewTopic
}

/// <summary>Steps of the task editor: the new task wizard and editing one field from the card.</summary>
public enum EditorStep
{
    Title,
    Date,
    Month,
    Day,

    /// <summary>The hour of the time, typed.</summary>
    Hour,

    /// <summary>The minutes of the time, typed once the hour is known.</summary>
    Minute,

    Repeat,
    Interval,
    Reminder,
    ReminderHours,
    Topic,
    NewTopicName
}

/// <summary>The field of an existing task being edited.</summary>
public enum TaskField
{
    Title,
    Date,
    Time,
    Repeat,
    Reminder,
    Topic
}

/// <summary>What the new task wizard has collected so far, or the value of the field being edited.</summary>
public class TaskDraft
{
    public string? Title { get; set; }
    public DateOnly? Date { get; set; }

    /// <summary>Month picked under "📆 Інша дата", as its first day.</summary>
    public DateOnly? Month { get; set; }

    /// <summary>Hour typed on the time step; the minutes are asked for next.</summary>
    public int? Hour { get; set; }

    public TimeOnly? Time { get; set; }
    public RepeatInput? Repeat { get; set; }
    public int? RemindMinutesBefore { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
}

/// <summary>
/// The planner's state in a chat: the list the user is in, the open card, or an editor step.
/// It is UI state, so it lives only in memory: a restart just starts from "Сьогодні" again.
/// </summary>
public class PlannerDialog : BotDialog
{
    public PlannerDialog(PlannerList list) => List = list;

    public PlannerList List { get; }

    /// <summary>Page of <see cref="List"/>.</summary>
    public int Page { get; set; }

    public PlannerScreen Screen { get; set; }

    /// <summary>The task whose card is open or which is being edited.</summary>
    public Guid? TaskId { get; set; }

    /// <summary>The editor's step; null while a list, a card or the settings are shown.</summary>
    public EditorStep? Step { get; set; }

    /// <summary>Null in the new task wizard.</summary>
    public TaskField? EditedField { get; set; }

    public TaskDraft Draft { get; } = new();

    public bool IsEditing => Step is not null;
    public bool IsNewTask => IsEditing && EditedField is null;

    /// <summary>A list is shown, so typed text is a new task (or a new topic on the topic list).</summary>
    public bool IsOnList => !IsEditing && Screen == PlannerScreen.List;

    public bool IsAskingTopicName => !IsEditing && Screen == PlannerScreen.NewTopic;
}
