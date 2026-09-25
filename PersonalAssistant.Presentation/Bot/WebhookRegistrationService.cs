
using Microsoft.Extensions.Options;
using PersonalAssistant.Presentation.Bot.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PersonalAssistant.Presentation.Bot;
public class WebhookRegistrationService : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<WebhookRegistrationService> _logger;

    public WebhookRegistrationService(ITelegramBotClient botClient, IOptions<TelegramOptions> options, ILogger<WebhookRegistrationService> logger)
    {
        _botClient = botClient;
        _options = options.Value;
        _logger = logger;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogInformation("WebhookUrl is not configured, skipping webhook registration.");
            return;
        }

        await _botClient.SetWebhook(
            url: _options.WebhookUrl,
            allowedUpdates: new[] { UpdateType.Message, UpdateType.CallbackQuery },
            secretToken: _options.SecretToken,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Webhook registered: {Url}", _options.WebhookUrl);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
