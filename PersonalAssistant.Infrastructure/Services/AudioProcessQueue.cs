using System.Threading.Channels;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Infrastructure.Services;

public class AudioProcessingQueue : IAudioProcessQueue
{
    private readonly Channel<Guid> _queue;

    public AudioProcessingQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };

        _queue = Channel.CreateUnbounded<Guid>(options);
    }

    public async ValueTask EnqueueAsync(Guid journalEntryId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(journalEntryId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}