using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace PersonalAssistant.Presentation.Services;

public class BotMessenger : IBotMessenger
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotMessenger> _logger;

    public BotMessenger(ITelegramBotClient botClient, ILogger<BotMessenger> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task SendAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        await _botClient.SendMessage(chatId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
    }

    public async Task EditAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            if (ex.Message.Contains("message is not modified"))
            {
                await SendAsync(chatId, text, replyMarkup, cancellationToken);
                return;
            }

            if (ex.Message.Contains("message to edit not found") || ex.Message.Contains("message can't be edited"))
            {
                await SendAsync(chatId, text, replyMarkup, cancellationToken);
                return;
            }

            _logger.LogWarning(ex, $"[BotNotifService] Telegram API error while editing message in chat {chatId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[BotNotifService] Telegram API error while editing message in chat {chatId}");
        }
    }
}
