using Microsoft.Extensions.Options;
using PersonalAssistant.Presentation.Bot.Options;
using Telegram.Bot.Types;

namespace PersonalAssistant.Presentation.Bot;
public class UpdateProcessingService : BackgroundService
{
    private readonly IUpdateQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramOptions _options;
    private readonly ILogger<UpdateProcessingService> _logger;
    private int _lastUpdateId;

    public UpdateProcessingService(IUpdateQueue queue, IServiceScopeFactory scopeFactory,
        IOptions<TelegramOptions> options, ILogger<UpdateProcessingService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var update = await _queue.DequeueAsync(stoppingToken);
                await ProcessAsync(update, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Unhandled error while processing update"); }
        }
    }

    private async Task ProcessAsync(Update update, CancellationToken ct)
    {
        // Telegram update ids only grow: anything <= last is a retry
        if (update.Id <= _lastUpdateId)
        {
            _logger.LogInformation("Skipping duplicate update {UpdateId}", update.Id);
            return;
        }
        _lastUpdateId = update.Id;

        var userId = update.Message?.From?.Id ?? update.CallbackQuery?.From.Id;
        if (userId != _options.OwnerId)
        {
            _logger.LogWarning("Ignoring update {UpdateId} from unauthorized user {UserId}", update.Id, userId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<IUpdateRouter>();
        await router.RouteAsync(update, ct);
    }
}
