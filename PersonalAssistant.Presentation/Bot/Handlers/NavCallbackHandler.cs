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
        BotConstants.Payloads.NavScraper
    };

    private readonly IBotMessenger _messenger;
    private readonly IDialogStore _dialogs;

    public NavigationCallbackHandler(IBotMessenger messenger, IDialogStore dialogs)
    {
        _messenger = messenger;
        _dialogs = dialogs;
    }

    public bool CanHandle(string payload) => Payloads.Contains(payload);

    public Task HandleAsync(CallbackContext context, CancellationToken cancellationToken)
    {
        // The window stays the same message, so a feature's unfinished input would still match it:
        // leaving for a menu ends it (the planner, for one, would take typed text as a new task)
        _dialogs.Remove(context.ChatId);

        var (text, menu) = context.Payload switch
        {
            BotConstants.Payloads.NavRoot    => (BotConstants.Message.MsgRootMenu, MenuBuilder.GetRootMenu()),
            BotConstants.Payloads.NavJournal => (BotConstants.Message.MsgJournalMenu, MenuBuilder.GetJournalMenu()),
            _                                => (BotConstants.Message.MsgNotImplementedFeature, MenuBuilder.GetNotImplementedFeature())
        };

        return _messenger.ShowScreenAsync(context.ChatId, text, menu, cancellationToken);
    }
}
