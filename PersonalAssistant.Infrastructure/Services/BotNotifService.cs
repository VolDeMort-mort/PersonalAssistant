using Telegram.Bot;
using PersonalAssistant.Application.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using PersonalAssistant.Infrastructure.Workers;
using Telegram.Bot.Exceptions;


namespace PersonalAssistant.Infrastructure.Services
{
    public class BotNotifService: IBotNotifService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<BotNotifService> _logger;

        public BotNotifService(ITelegramBotClient botClient, ILogger<BotNotifService> logger)
        {
            _botClient = botClient;
            _logger = logger;
        }

        public async Task SendMessageAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
        {
            await _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken, replyMarkup: replyMarkup);
        }

        public async Task EditMessageAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
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
                    await SendMessageAsync(chatId, text, replyMarkup, cancellationToken);
                    return;
                }

                if (ex.Message.Contains("message to edit not found") || ex.Message.Contains("message can't be edited"))
                {
                    await SendMessageAsync(chatId, text, replyMarkup, cancellationToken);
                    return;
                }

                _logger.LogWarning(ex, $"[BotNotifService] Telegram API error while editing message in chat {chatId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"[BotNotifService] Telegram API error while editing message in chat {chatId}");
            }

        }

        public async Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _botClient.DeleteMessage(chatId, messageId, cancellationToken);
            }
            catch (Exception ex) 
            {
                _logger.LogWarning(ex, $"[BotNotifService] Couldnt delete a message {messageId} in {chatId}");
            }
        }
    }
}
