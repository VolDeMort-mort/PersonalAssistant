using PersonalAssistant.Presentation.Constants;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Presentation.Helpers;

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

    public NavigationCallbackHandler(IBotMessenger messenger)
    {
        _messenger = messenger;
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

        return _messenger.ShowScreenAsync(context.ChatId, text, menu, cancellationToken);
    }
}
