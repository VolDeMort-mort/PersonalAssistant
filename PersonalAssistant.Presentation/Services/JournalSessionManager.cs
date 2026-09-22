using System.Collections.Concurrent;

namespace PersonalAssistant.Presentation.Services;

public class JournalSessionManager
{
    private class ActiveSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public List<SessionMessageDto> Messages { get; set; } = new();
    }

    // Temporary list of messages with ChatId
    private readonly ConcurrentDictionary<long, ActiveSession> _activeSessions = new();

    public bool IsRecording(long chatId) => _activeSessions.ContainsKey(chatId);

    public void StartSession(long chatId)
    {
        _activeSessions[chatId] = new ActiveSession();
    }

    public void AddMessage(long chatId, SessionMessageDto message)
    {
        if (_activeSessions.TryGetValue(chatId, out var session))
        {
            session.Messages.Add(message);
        }
    }

    /// <summary>
    /// Stops the listening session. 
    /// Merges all recorded lines splited by "\n"
    /// </summary>
    public (Guid SessionId, List<SessionMessageDto> Messages)? EndSession(long chatId)
    {
        if (_activeSessions.TryRemove(chatId, out var session))
        {
            return (session.SessionId, session.Messages);
        }
        return null;
    }
}