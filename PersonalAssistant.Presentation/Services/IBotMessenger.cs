using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Services;

public interface IBotMessenger
{
    Task SendAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);
    Task EditAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default);
}
