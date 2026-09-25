using PersonalAssistant.Presentation.Bot.Handlers;
using PersonalAssistant.Presentation.Bot;
using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Presentation.Bot.Handlers;
public class MenuCommandHandler: IMessageHandler
{
    private readonly IBotMessenger _messenger;
    private readonly IUserStateManager _stateManager;

    public MenuCommandHandler(IBotMessenger messenger, IUserStateManager stateManager)
    {
        _messenger = messenger;
        _stateManager = stateManager;
    }
    public Task<bool> CanHandleAsync(MessageContext context, CancellationToken ct) =>
        Task.FromResult(context.Message.Text?.StartsWith(BotConstants.Commands.MainMenu, StringComparison.OrdinalIgnoreCase) == true);

    public async Task HandleAsync(MessageContext context, CancellationToken ct)
    {
        _stateManager.ClearState(context.ChatId);
        await _messenger.SendAsync(context.ChatId, BotConstants.Message.MsgRootMenu, MenuBuilder.GetRootMenu(), ct);
    }

}