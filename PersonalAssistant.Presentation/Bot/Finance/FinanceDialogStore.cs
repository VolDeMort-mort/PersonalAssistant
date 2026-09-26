using System.Collections.Concurrent;

namespace PersonalAssistant.Presentation.Bot.Finance;

/// <summary>At most one unfinished finance input per chat.</summary>
public interface IFinanceDialogStore
{
    FinanceDialog? Get(long chatId);
    void Set(long chatId, FinanceDialog dialog);
    void Remove(long chatId);
}

public class FinanceDialogStore : IFinanceDialogStore
{
    private readonly ConcurrentDictionary<long, FinanceDialog> _dialogs = new();

    public FinanceDialog? Get(long chatId) => _dialogs.TryGetValue(chatId, out var dialog) ? dialog : null;
    public void Set(long chatId, FinanceDialog dialog) => _dialogs[chatId] = dialog;
    public void Remove(long chatId) => _dialogs.TryRemove(chatId, out _);
}
