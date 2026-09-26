using System.Threading.Channels;
using Telegram.Bot.Types;

namespace PersonalAssistant.Presentation.Bot;

public interface IUpdateQueue
{
    bool TryEnqueue(Update update);
    ValueTask<Update> DequeueAsync(CancellationToken cancellationToken);
}

public class UpdateQueue : IUpdateQueue
{
    private readonly Channel<Update> _channel = Channel.CreateBounded<Update>(
        new BoundedChannelOptions(100) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public bool TryEnqueue(Update update) => _channel.Writer.TryWrite(update);
    public ValueTask<Update> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
}
