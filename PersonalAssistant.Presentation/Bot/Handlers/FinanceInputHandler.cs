using PersonalAssistant.Presentation.Bot.Finance;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Handlers;

/// <summary>
/// Text the user types while a finance dialog waits for it: amount, title, comment, etc.
/// </summary>
public class FinanceInputHandler : IMessageHandler
{
    private readonly FinanceFlow _flow;
    private readonly IBotMessenger _messenger;

    public FinanceInputHandler(FinanceFlow flow, IBotMessenger messenger)
    {
        _flow = flow;
        _messenger = messenger;
    }

    public Task<bool> CanHandleAsync(MessageContext context, CancellationToken ct) =>
        Task.FromResult(_flow.IsWaitingForText(context.ChatId));

    public async Task HandleAsync(MessageContext context, CancellationToken ct)
    {
        await _flow.HandleTextAsync(context.ChatId, context.Message.Text, ct);

        // Only after processing: if saving fails, the typed text is still in the chat
        await _messenger.DeleteAsync(context.ChatId, context.MessageId, ct);
    }
}
