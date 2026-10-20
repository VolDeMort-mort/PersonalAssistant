using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalAssistant.Application.Features.Planner.Commands;

namespace PersonalAssistant.Infrastructure.Workers;

/// <summary>
/// Only a clock: once a minute it runs the planner's timed use cases. What to move, remind about
/// or list is decided by the commands and their notifier, not here.
/// </summary>
public class PlannerWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    // The order matters: overdue tasks move to today first, so reminders and the morning list see them there
    private static readonly IRequest<int>[] UseCases =
    {
        new RollOverOverdueTasksCommand(),
        new SendTaskRemindersCommand(),
        new SendDailyAgendaCommand()
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlannerWorker> _logger;

    public PlannerWorker(IServiceScopeFactory scopeFactory, ILogger<PlannerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                foreach (var useCase in UseCases)
                    await RunAsync(useCase, stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is stopping
        }
    }

    private async Task RunAsync(IRequest<int> useCase, CancellationToken stoppingToken)
    {
        var name = useCase.GetType().Name;
        try
        {
            // A scope per use case: a failed one must not leave its unsaved changes to the next
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var count = await sender.Send(useCase, stoppingToken);
            if (count > 0)
                _logger.LogInformation("{UseCase} handled {Count} item(s)", name, count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One failed use case must stop neither the others nor the clock: the next run retries
            _logger.LogError(ex, "{UseCase} failed, retrying in {Interval}", name, Interval);
        }
    }
}
