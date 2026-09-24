using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

using PersonalAssistant.Application.Interfaces;
using System.Collections.Concurrent;

namespace PersonalAssistant.Infrastructure.Services;

public class UserStateManager: IUserStateManager
{
    private readonly ConcurrentDictionary<long, UserState> _states = new();

    public void SetState(long chatId, UserState state)
    {
        _states[chatId] = state;
    }

    public UserState GetState(long chatId)
    {
        return _states.TryGetValue(chatId, out var state) ? state : UserState.None;
    }

    public void ClearState(long chatId)
    {
        _states.TryRemove(chatId, out _);
    }
}
