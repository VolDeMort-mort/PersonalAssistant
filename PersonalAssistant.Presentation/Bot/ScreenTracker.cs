using System.Collections.Concurrent;

namespace PersonalAssistant.Presentation.Bot;

/// <summary>
/// Remembers which bot message is the "window" of each chat,
/// so every screen is drawn into that one message instead of sending new ones.
/// </summary>
public interface IScreenTracker
{
    int? Get(long chatId);
    void Set(long chatId, int messageId);
}

public class ScreenTracker : IScreenTracker
{
    private readonly ConcurrentDictionary<long, int> _screens = new();

    public int? Get(long chatId) => _screens.TryGetValue(chatId, out var messageId) ? messageId : null;
    public void Set(long chatId, int messageId) => _screens[chatId] = messageId;
}
