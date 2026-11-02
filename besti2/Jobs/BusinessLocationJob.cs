using besti2.Application.Businesses;
using besti2.Infrastructure.Services;

namespace besti2.Jobs;

// Once an hour: businesses without a map position are looked up in Geonorge again. When Geonorge is down at
// sign-up, the business would otherwise stay off the map and out of "Nær meg" until it changes its address
public class BusinessLocationJob(IServiceScopeFactory scopeFactory, ILogger<BusinessLocationJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var businesses = scope.ServiceProvider.GetRequiredService<IBusinessService>();
                var found = await businesses.FillMissingLocationsAsync(stoppingToken);
                if (found > 0)
                {
                    logger.LogInformation("Found the map position for {Count} businesses", found);
                }

                JobHeartbeats.Ran("Kart", true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                JobHeartbeats.Ran("Kart", false);
                logger.LogError(ex, "Looking up missing map positions failed");
            }
        }
    }
}
