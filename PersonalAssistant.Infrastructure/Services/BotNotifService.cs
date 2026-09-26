using Telegram.Bot;
using PersonalAssistant.Application.Interfaces;
using Microsoft.Extensions.Logging;


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

        public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            await _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
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
