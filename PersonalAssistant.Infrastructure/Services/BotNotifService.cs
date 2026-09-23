using Telegram.Bot;
using PersonalAssistant.Application.Interfaces;


namespace PersonalAssistant.Infrastructure.Services
{
    public class BotNotifService: IBotNotifService
    {
        private readonly ITelegramBotClient _botClient;

        public BotNotifService(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            await _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }

    }
}
