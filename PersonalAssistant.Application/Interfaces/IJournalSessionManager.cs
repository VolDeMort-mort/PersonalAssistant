using PersonalAssistant.Application.Features.Journal.Commands;

namespace PersonalAssistant.Application.Interfaces;

public interface IJournalSessionManager
{
    bool IsRecording(long chatId);
    void StartSession(long chatId);
    void AddMessage(long chatId, SessionMessageDto message);
    (Guid SessionId, List<SessionMessageDto> Messages)? EndSession(long chatId);
}