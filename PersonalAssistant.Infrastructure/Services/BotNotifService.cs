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
            catch (ApiRequestException) 
            {
                await SendMessageAsync(
                    chatId: chatId,
                    text: text,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            catch(Exception ex) {
                _logger.LogWarning($"Catched an {ex}");
            }

        }

    }
}
