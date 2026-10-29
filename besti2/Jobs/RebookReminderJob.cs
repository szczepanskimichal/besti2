using besti2.Application.Bookings;

namespace besti2.Jobs;

// "Tid for ny time?": every hour (only 9-20 in Oslo) reminds customers who asked for it to book again
public class RebookReminderJob(IServiceScopeFactory scopeFactory, ILogger<RebookReminderJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var reminders = scope.ServiceProvider.GetRequiredService<IRebookReminderService>();
                    var sent = await reminders.SendDueAsync(DateTime.UtcNow, stoppingToken);
                    if (sent > 0)
                    {
                        logger.LogInformation("Sent {Count} Tid for ny time reminders", sent);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Sending Tid for ny time reminders failed");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The API is stopping
        }
    }
}
