namespace PersonalAssistant.Presentation.Bot.Handlers;
public interface ICallbackHandler
{
    bool CanHandle(string payload);
    Task HandleAsync(CallbackContext context, CancellationToken cancellationToken);
}
