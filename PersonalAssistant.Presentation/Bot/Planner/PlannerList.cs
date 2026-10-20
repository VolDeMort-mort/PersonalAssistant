namespace PersonalAssistant.Presentation.Bot.Planner;

public enum PlannerListKind
{
    Day,
    Week,
    Topics,
    Topic
}

/// <summary>
/// The planner list the user is in: where "🔙" from a card returns to and where quick add puts a typed task.
/// </summary>
/// <param name="FromWeek">A day opened from the week view: its "🔙 Назад" returns to the week.</param>
public record PlannerList(PlannerListKind Kind, DateOnly? Date = null, Guid? TopicId = null, bool FromWeek = false)
{
    public static PlannerList Day(DateOnly date, bool fromWeek = false) => new(PlannerListKind.Day, Date: date, FromWeek: fromWeek);
    public static PlannerList Topic(Guid topicId) => new(PlannerListKind.Topic, TopicId: topicId);
    public static readonly PlannerList Week = new(PlannerListKind.Week);
    public static readonly PlannerList Topics = new(PlannerListKind.Topics);

    /// <summary>A day or a topic: typed text there becomes a task of that list.</summary>
    public bool TakesQuickAdd => Kind is PlannerListKind.Day or PlannerListKind.Topic;
}
