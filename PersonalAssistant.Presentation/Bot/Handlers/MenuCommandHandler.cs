using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Presentation.Helpers;

namespace PersonalAssistant.Presentation.Bot.Handlers;

public class MenuCommandHandler : IMessageHandler
{
    private readonly IBotMessenger _messenger;

    public MenuCommandHandler(IBotMessenger messenger)
    {
        _messenger = messenger;
    }

    public Task<bool> CanHandleAsync(MessageContext context, CancellationToken ct) =>
        Task.FromResult(context.Message.Text?.StartsWith(BotConstants.Commands.MainMenu, StringComparison.OrdinalIgnoreCase) == true);

    public async Task HandleAsync(MessageContext context, CancellationToken ct)
    {
        await _messenger.DeleteAsync(context.ChatId, context.MessageId, ct);
        await _messenger.OpenScreenAsync(context.ChatId, BotConstants.Message.MsgRootMenu, MenuBuilder.GetRootMenu(), ct);
    }
}
