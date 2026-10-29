using besti2.Application.Bookings;
using besti2.Infrastructure.Services;

namespace besti2.Jobs;

// "Si fra hvis det blir ledig": every 2 minutes looks whether a time became free for the waiting customers
// (a cancellation, a moved visit, blocked time removed, longer opening hours) and tells them at once
public class WaitlistJob(IServiceScopeFactory scopeFactory, ILogger<WaitlistJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        try
        {
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var waitlist = scope.ServiceProvider.GetRequiredService<IWaitlistService>();
                    await waitlist.CheckAsync(DateTime.UtcNow, stoppingToken);
                    JobHeartbeats.Ran("Venteliste", true);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    JobHeartbeats.Ran("Venteliste", false);
                    // The database is down for a moment: never crash the API, try again next round
                    logger.LogError(ex, "Checking the waitlist failed");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The API is stopping
        }
    }
}
