using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Application.Features.Journal.Commands;
using MediatR;

namespace PersonalAssistant.Infrastructure.Workers;

public class AudioProcessWorker : BackgroundService
{
    private readonly ILogger<AudioProcessWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAudioProcessQueue _queue;
    
    public AudioProcessWorker(
        ILogger<AudioProcessWorker> logger,
        IServiceScopeFactory scopeFactory,
        IAudioProcessQueue queue)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audio worker started");

        // Media left unprocessed by a previous run (e.g. the app stopped mid-download)
        await SweepUnprocessedFilesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entryId = await _queue.DequeueAsync(stoppingToken);

                await ProcessSingleAudioFileAsync(entryId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Audio worker stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio worker failed to process a journal entry");
            }
        }
    }

    private async Task SweepUnprocessedFilesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJournalRepository>();

        var unprocessedEntries = await repository.GetUnprocessedMediaAsync(stoppingToken);

        var count = 0;
        foreach (var entry in unprocessedEntries)
        {
            await _queue.EnqueueAsync(entry.Id, stoppingToken);
            count++;
        }

        _logger.LogInformation("Queued {Count} unprocessed media file(s) from a previous run", count);
    }

    private async Task ProcessSingleAudioFileAsync(Guid entryId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new ProcessJournalMediaCommand(entryId), stoppingToken);
    }
}