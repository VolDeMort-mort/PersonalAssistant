namespace PersonalAssistant.Presentation.Bot.Planner;

public enum PlannerListKind
{
    Day,
    Week,
    Undated,
    Topics,
    Topic
}

/// <summary>
/// The planner list the user is in: where "🔙" from a card returns to and where quick add puts a typed task.
/// </summary>
public record PlannerList(PlannerListKind Kind, DateOnly? Date = null, Guid? TopicId = null)
{
    public static PlannerList Day(DateOnly date) => new(PlannerListKind.Day, Date: date);
    public static PlannerList Topic(Guid topicId) => new(PlannerListKind.Topic, TopicId: topicId);
    public static readonly PlannerList Week = new(PlannerListKind.Week);
    public static readonly PlannerList Undated = new(PlannerListKind.Undated);
    public static readonly PlannerList Topics = new(PlannerListKind.Topics);

    /// <summary>A day, "no date" or a topic: typed text there becomes a task of that list.</summary>
    public bool TakesQuickAdd => Kind is PlannerListKind.Day or PlannerListKind.Undated or PlannerListKind.Topic;
}
