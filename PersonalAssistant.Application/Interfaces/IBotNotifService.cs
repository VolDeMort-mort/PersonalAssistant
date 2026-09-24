using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Application.Interfaces;

public interface IBotNotifService
{
    Task SendMessageAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);
    Task EditMessageAsync(long chatId, int messageId, string newText, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken = default);
}
