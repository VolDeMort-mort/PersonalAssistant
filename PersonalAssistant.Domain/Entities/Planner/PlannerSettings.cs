namespace PersonalAssistant.Domain.Entities.Planner;

/// <summary>
/// Planner preferences of a chat. For now: when to send the morning list of today's tasks.
/// </summary>
public class PlannerSettings
{
    public const int DefaultAgendaHour = 8;

    /// <summary>
    /// How late the morning list may still come, e.g. after a restart. Later that day it is skipped:
    /// "today's tasks" in the evening would be noise.
    /// </summary>
    public static readonly TimeSpan AgendaWindow = TimeSpan.FromHours(2);

    private PlannerSettings() { }

    public long ChatId { get; private set; }

    /// <summary>Hour in the user's time zone; null when the morning list is turned off.</summary>
    public int? AgendaHour { get; private set; }

    /// <summary>Local date the last morning list was sent for: at most one a day.</summary>
    public DateOnly? LastAgendaSentOn { get; private set; }

    public static PlannerSettings Create(long chatId) =>
        new() { ChatId = chatId, AgendaHour = DefaultAgendaHour };

    /// <param name="hour">Null turns the morning list off.</param>
    public void SetAgendaHour(int? hour)
    {
        if (hour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), hour, "Must be from 0 to 23.");

        AgendaHour = hour;
    }

    /// <param name="localNow">Now in the user's time zone.</param>
    public bool IsAgendaDue(DateTime localNow)
    {
        if (AgendaHour is not { } hour || LastAgendaSentOn == DateOnly.FromDateTime(localNow))
            return false;

        var sendAt = localNow.Date.AddHours(hour);
        return localNow >= sendAt && localNow < sendAt + AgendaWindow;
    }

    /// <summary>
    /// Also called on a day with nothing to list: the list for that day is done either way.
    /// </summary>
    public void MarkAgendaSent(DateOnly today) => LastAgendaSentOn = today;
}
