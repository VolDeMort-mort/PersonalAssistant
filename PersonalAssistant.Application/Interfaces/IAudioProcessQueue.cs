namespace PersonalAssistant.Application.Interfaces;

public interface IAudioProcessQueue
{
    ValueTask EnqueueAsync(Guid journalEntryId, CancellationToken cancellationToken = default);

    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default);
}
