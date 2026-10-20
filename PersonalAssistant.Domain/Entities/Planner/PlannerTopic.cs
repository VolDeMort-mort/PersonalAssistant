using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Entities.Planner;

/// <summary>
/// An area of life tasks belong to, e.g. "💼 Робота" or "🏠 Дім". A task may have no topic at all.
/// </summary>
public class PlannerTopic
{
    public const int MaxNameLength = 40;

    private PlannerTopic() { }

    public Guid Id { get; private set; }
    public long ChatId { get; private set; }
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }

    public static PlannerTopic Create(long chatId, string name, int sortOrder)
    {
        return new PlannerTopic
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Name = Guard.RequiredText(name, MaxNameLength, nameof(name)),
            SortOrder = sortOrder
        };
    }

    /// <summary>Same topic for a human: "робота" matches "💼 Робота".</summary>
    public bool HasSameName(string name) => DisplayName.AreSame(Name, name);
}
