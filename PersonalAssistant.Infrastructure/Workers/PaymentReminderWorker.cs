using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalAssistant.Application.Features.Finance.Commands;

namespace PersonalAssistant.Infrastructure.Workers;

/// <summary>
/// Only a clock: once a minute it runs the reminder use case. What to remind about and how
/// is decided by <see cref="SendPaymentRemindersCommand"/> and its notifier, not here.
/// </summary>
public class PaymentReminderWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentReminderWorker> _logger;

    public PaymentReminderWorker(IServiceScopeFactory scopeFactory, ILogger<PaymentReminderWorker> logger)
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
                await SendDueRemindersAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is stopping
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Scoped services (DbContext, repositories) live for one run only
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var sent = await sender.Send(new SendPaymentRemindersCommand(), stoppingToken);
            if (sent > 0)
                _logger.LogInformation("Sent {Count} payment reminder(s)", sent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One failed run must not stop the clock: the next run retries unsent reminders
            _logger.LogError(ex, "Sending payment reminders failed, retrying in {Interval}", Interval);
        }
    }
}
