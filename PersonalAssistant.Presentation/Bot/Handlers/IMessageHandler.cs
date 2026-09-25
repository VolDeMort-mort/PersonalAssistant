namespace PersonalAssistant.Presentation.Bot.Handlers;
public interface IMessageHandler
{
    Task<bool> CanHandleAsync(MessageContext context, CancellationToken cancellationToken);
    Task HandleAsync(MessageContext context, CancellationToken cancellationToken);
}
