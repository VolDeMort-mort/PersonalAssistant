namespace PersonalAssistant.Application.Interfaces;

public interface IBotNotifService
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken = default);
}
