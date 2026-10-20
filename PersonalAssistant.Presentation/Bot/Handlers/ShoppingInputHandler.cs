using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Presentation.Bot.Finance;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Handlers;

/// <summary>
/// Text typed while the shopping list is on screen: new items on the list itself,
/// an amount or a category while buying, a title, price or link in the wizard or when editing.
/// </summary>
public class ShoppingInputHandler : IMessageHandler
{
    private readonly ShoppingFlow _flow;
    private readonly ShoppingEditorFlow _editor;
    private readonly IBotMessenger _messenger;

    public ShoppingInputHandler(ShoppingFlow flow, ShoppingEditorFlow editor, IBotMessenger messenger)
    {
        _flow = flow;
        _editor = editor;
        _messenger = messenger;
    }

    public Task<bool> CanHandleAsync(MessageContext context, CancellationToken ct) =>
        Task.FromResult(_flow.IsWaitingForText(context.ChatId));

    public async Task HandleAsync(MessageContext context, CancellationToken ct)
    {
        try
        {
            if (_flow.HandlesText(context.ChatId))
                await _flow.HandleTextAsync(context.ChatId, context.Message.Text, ct);
            else
                await _editor.HandleTextAsync(context.ChatId, context.Message.Text, ct);
        }
        catch (NotFoundException)
        {
            // E.g. renaming an item that was deleted meanwhile
            await _flow.ShowListAsync(context.ChatId, FinanceView.GoneNotice, ct);
        }

        // Only after processing: if saving fails, the typed text is still in the chat
        await _messenger.DeleteAsync(context.ChatId, context.MessageId, ct);
    }
}
