using System.Collections.Concurrent;

namespace PersonalAssistant.Presentation.Services;

public class JournalSessionManager
{
    // Temporary list of messages with ChatId
    private readonly ConcurrentDictionary<long, List<string>> _activeSessions = new();

    public bool IsRecording(long chatId) => _activeSessions.ContainsKey(chatId);

    public void StartSession(long chatId)
    {
        _activeSessions[chatId] = new List<string>();
    }

    public void AddMessage(long chatId, string message)
    {
        if (_activeSessions.TryGetValue(chatId, out var messages))
        {
            messages.Add(message);
        }
    }

    /// <summary>
    /// Stops the listening session. 
    /// Merges all recorded lines splited by "\n"
    /// </summary>
    public string EndSessionAndGetText(long chatId)
    {
        if (_activeSessions.TryRemove(chatId, out var messages))
        {
            return string.Join("\n", messages);
        }
        return string.Empty;
    }
}