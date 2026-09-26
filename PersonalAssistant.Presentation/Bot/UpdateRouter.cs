using MediatR;
using PersonalAssistant.Application.Features.Journal.Queries;
using PersonalAssistant.Presentation.Bot.Handlers;
using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Services;

using Telegram.Bot.Types;

namespace PersonalAssistant.Presentation.Bot;

public interface IUpdateRouter
{
    Task RouteAsync(Update update, CancellationToken cancellationToken);
}

public class UpdateRouter : IUpdateRouter
{
    // While a journal is recording, only these buttons work: the rest of the bot is locked
    private static readonly HashSet<string> AllowedWhileRecording = new()
    {
        BotConstants.Payloads.NavJournalRecorded,
        BotConstants.Payloads.NavJournalCancelRecord
    };

    private readonly IEnumerable<ICallbackHandler> _callbackHandlers;
    private readonly IEnumerable<IMessageHandler> _messageHandlers;
    private readonly IBotMessenger _messenger;
    private readonly IScreenTracker _screens;
    private readonly ISender _sender;
    private readonly ILogger<UpdateRouter> _logger;

    public UpdateRouter(IEnumerable<ICallbackHandler> callbackHandlers, IEnumerable<IMessageHandler> messageHandlers,
        IBotMessenger messenger, IScreenTracker screens, ISender sender, ILogger<UpdateRouter> logger)
    {
        _callbackHandlers = callbackHandlers;
        _messageHandlers = messageHandlers;
        _messenger = messenger;
        _screens = screens;
        _sender = sender;
        _logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        if (update.CallbackQuery is { Message: not null, Data: not null } query)
        {
            var context = new CallbackContext(query.Message.Chat.Id, query.Message.MessageId, query.Data);
            await RouteCallbackAsync(query.Id, context, ct);
            return;
        }

        if (update.Message is { } message)
            await RouteMessageAsync(new MessageContext(message.Chat.Id, message.MessageId, message), ct);
    }

    private async Task RouteCallbackAsync(string queryId, CallbackContext context, CancellationToken ct)
    {
        if (!AllowedWhileRecording.Contains(context.Payload) && await IsJournalRecordingAsync(context.ChatId, ct))
        {
            await _messenger.AnswerCallbackAsync(queryId, BotConstants.Message.MsgJournalLocked, cancellationToken: ct);
            return;
        }

        await _messenger.AnswerCallbackAsync(queryId, cancellationToken: ct);

        // After a restart nothing is tracked: adopt the pressed message as the chat's window
        if (_screens.Get(context.ChatId) is null)
            _screens.Set(context.ChatId, context.MessageId);

        var handler = _callbackHandlers.FirstOrDefault(h => h.CanHandle(context.Payload));
        if (handler is null)
            _logger.LogWarning("No handler for callback payload {Payload}", context.Payload);
        else
            await handler.HandleAsync(context, ct);
    }

    private async Task RouteMessageAsync(MessageContext context, CancellationToken ct)
    {
        // Commands like /menu would leave the journal mid-recording: drop them
        if (IsCommand(context.Message) && await IsJournalRecordingAsync(context.ChatId, ct))
        {
            await _messenger.DeleteAsync(context.ChatId, context.MessageId, ct);
            return;
        }

        foreach (var handler in _messageHandlers)
        {
            if (!await handler.CanHandleAsync(context, ct)) continue;
            await handler.HandleAsync(context, ct);
            return;
        }
    }

    private static bool IsCommand(Message message) => message.Text?.StartsWith('/') == true;

    private Task<bool> IsJournalRecordingAsync(long chatId, CancellationToken ct) =>
        _sender.Send(new IsJournalRecordingQuery(chatId), ct);
}
