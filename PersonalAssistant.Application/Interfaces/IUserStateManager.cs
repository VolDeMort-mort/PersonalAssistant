namespace PersonalAssistant.Application.Interfaces;

public enum UserState
{
    None,
    Journaling,
    WaitingForTask,
    WaitingForUrl
}                      


public interface IUserStateManager
{
    void SetState(long chatId, UserState state);
    UserState GetState(long chatId);
    void ClearState(long chatId);
}
