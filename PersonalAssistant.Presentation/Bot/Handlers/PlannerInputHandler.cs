using PersonalAssistant.Application.Common.Exceptions;
using PersonalAssistant.Presentation.Bot.Planner;
using PersonalAssistant.Presentation.Services;

namespace PersonalAssistant.Presentation.Bot.Handlers;

/// <summary>
/// Text typed while the planner is on screen: a task title or a topic name in the editor,
/// otherwise a quick-added task of the open list.
/// </summary>
public class PlannerInputHandler : IMessageHandler
{
    private readonly PlannerFlow _planner;
    private readonly TaskEditorFlow _editor;
    private readonly IBotMessenger _messenger;

    public PlannerInputHandler(PlannerFlow planner, TaskEditorFlow editor, IBotMessenger messenger)
    {
        _planner = planner;
        _editor = editor;
        _messenger = messenger;
    }

    public Task<bool> CanHandleAsync(MessageContext context, CancellationToken ct) =>
        Task.FromResult(_planner.IsWaitingForText(context.ChatId));

    public async Task HandleAsync(MessageContext context, CancellationToken ct)
    {
        try
        {
            if (_editor.IsEditing(context.ChatId))
                await _editor.HandleTextAsync(context.ChatId, context.Message.Text, ct);
            else
                await _planner.HandleTextAsync(context.ChatId, context.Message.Text, ct);
        }
        catch (NotFoundException)
        {
            // E.g. renaming a task that was deleted meanwhile
            await _planner.ShowHomeAsync(context.ChatId, PlannerView.GoneNotice, ct);
        }

        // Only after processing: if saving fails, the typed text is still in the chat
        await _messenger.DeleteAsync(context.ChatId, context.MessageId, ct);
    }
}
