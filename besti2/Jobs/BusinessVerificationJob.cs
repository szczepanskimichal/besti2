using besti2.Application.Businesses;
using besti2.Infrastructure.Services;

namespace besti2.Jobs;

// Runs at startup and then once a day: "Verifisert bedrift" is checked again in Brønnøysund every week
public class BusinessVerificationJob(IServiceScopeFactory scopeFactory, ILogger<BusinessVerificationJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                // A background job lives for the whole app, so it needs its own scope for the DbContext
                using var scope = scopeFactory.CreateScope();
                var verifier = scope.ServiceProvider.GetRequiredService<IBusinessVerifier>();
                await verifier.RecheckAsync(DateTime.UtcNow, stoppingToken);
                JobHeartbeats.Ran("Brreg-sjekk", true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                JobHeartbeats.Ran("Brreg-sjekk", false);
                // Never crash the API because of the check; try again tomorrow
                logger.LogError(ex, "Checking businesses in Brønnøysund failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
