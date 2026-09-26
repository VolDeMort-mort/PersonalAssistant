using System.Collections.Concurrent;

namespace PersonalAssistant.Presentation.Bot;

/// <summary>At most one unfinished input per chat, whichever feature it belongs to.</summary>
public interface IDialogStore
{
    /// <summary>The chat's dialog if it is of type <typeparamref name="T"/>.</summary>
    T? Get<T>(long chatId) where T : BotDialog;

    void Set(long chatId, BotDialog dialog);
    void Remove(long chatId);
}

public class DialogStore : IDialogStore
{
    private readonly ConcurrentDictionary<long, BotDialog> _dialogs = new();

    public T? Get<T>(long chatId) where T : BotDialog =>
        _dialogs.TryGetValue(chatId, out var dialog) ? dialog as T : null;

    public void Set(long chatId, BotDialog dialog) => _dialogs[chatId] = dialog;
    public void Remove(long chatId) => _dialogs.TryRemove(chatId, out _);
}
