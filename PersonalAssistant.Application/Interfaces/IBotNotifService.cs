namespace PersonalAssistant.Application.Interfaces;

public interface IBotNotifService
{
    Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
