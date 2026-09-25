using PersonalAssistant.Presentation.Bot.Handlers;
using PersonalAssistant.Presentation.Services;

using Telegram.Bot.Types;

namespace PersonalAssistant.Presentation.Bot;

public interface IUpdateRouter
{
    Task RouteAsync(Update update, CancellationToken cancellationToken);
}

public class UpdateRouter : IUpdateRouter
{
    private readonly IEnumerable<ICallbackHandler> _callbackHandlers;
    private readonly IEnumerable<IMessageHandler> _messageHandlers;
    private readonly IBotMessenger _messenger;
    private readonly ILogger<UpdateRouter> _logger;

    public UpdateRouter(IEnumerable<ICallbackHandler> callbackHandlers, IEnumerable<IMessageHandler> messageHandlers, IBotMessenger messenger, ILogger<UpdateRouter> logger)
    {
        _callbackHandlers = callbackHandlers;
        _messageHandlers = messageHandlers;
        _messenger = messenger;
        _logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        if (update.CallbackQuery is { Message: not null, Data: not null } query)
        {
            await _messenger.AnswerCallbackAsync(query.Id, ct);

            var context = new CallbackContext(query.Message.Chat.Id, query.Message.MessageId, query.Data);
            var handler = _callbackHandlers.FirstOrDefault(h => h.CanHandle(context.Payload));

            if (handler is null)
                _logger.LogWarning("No handler for callback payload {Payload}", context.Payload);
            else
                await handler.HandleAsync(context, ct);
            return;
        }

        if (update.Message is { } message)
        {
            var context = new MessageContext(message.Chat.Id, message.MessageId, message);
            foreach (var handler in _messageHandlers)
            {
                if (!await handler.CanHandleAsync(context, ct)) continue;
                await handler.HandleAsync(context, ct);
                return;
            }
        }
    }
}
