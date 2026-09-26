using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Presentation.Helpers;
using PersonalAssistant.Application.Interfaces;

namespace PersonalAssistant.Presentation.Bot.Handlers;
public class NavigationCallbackHandler : ICallbackHandler
{
    private static readonly HashSet<string> Payloads = new()
    {
        BotConstants.Payloads.NavRoot, 
        BotConstants.Payloads.NavJournal,
        BotConstants.Payloads.NavPlanner, 
        BotConstants.Payloads.NavScraper
    };

    private readonly IBotMessenger _messenger;
    private readonly IUserStateManager _stateManager;

    public NavigationCallbackHandler(IBotMessenger messenger, IUserStateManager stateManager)
    {
        _messenger = messenger;
        _stateManager = stateManager;
    }

    public bool CanHandle(string payload) => Payloads.Contains(payload);

    public Task HandleAsync(CallbackContext context, CancellationToken cancellationToken)
    {
        var (text, menu) = context.Payload switch
        {
            BotConstants.Payloads.NavRoot    => (BotConstants.Message.MsgRootMenu, MenuBuilder.GetRootMenu()),
            BotConstants.Payloads.NavJournal => (BotConstants.Message.MsgJournalMenu, MenuBuilder.GetJournalMenu()),
            _                                => (BotConstants.Message.MsgNotImplementedFeature, MenuBuilder.GetNotImplementedFeature())
        };

        if (context.Payload == BotConstants.Payloads.NavRoot)
            _stateManager.ClearState(context.ChatId);

        return _messenger.EditAsync(context.ChatId, context.MessageId, text, menu, cancellationToken);
    }
}
