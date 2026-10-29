using besti2.Application.Notifications;
using besti2.Infrastructure.Persistence;

namespace besti2.Jobs;

// "Varsler på telefonen": sends each new bell notification as a push message right after it is saved
// (see NotificationSignals.Added), and looks again every 30 seconds in case a signal was missed
public class PushSenderJob(IServiceScopeFactory scopeFactory, ILogger<PushSenderJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int taken;
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var push = scope.ServiceProvider.GetRequiredService<IPushService>();
                    taken = await push.SendPendingAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The database is down for a moment: never crash the API, try again in 30 seconds
                    logger.LogError(ex, "Sending push messages failed");
                    taken = 0;
                }

                // A full batch: maybe more are waiting, so no pause
                if (taken < Infrastructure.Services.PushService.BatchSize)
                {
                    await NotificationSignals.WaitForNewAsync(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The API is stopping
        }
    }
}
