using besti2.Application.Bookings;

namespace besti2.Jobs;

// Runs at startup and then once a day: anonymizes bookings older than 5 years (see /personvern)
public class BookingAnonymizationJob(IServiceScopeFactory scopeFactory, ILogger<BookingAnonymizationJob> logger)
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
                var anonymizer = scope.ServiceProvider.GetRequiredService<IBookingAnonymizer>();
                var count = await anonymizer.AnonymizeOldBookingsAsync(DateTime.UtcNow, stoppingToken);
                if (count > 0)
                {
                    logger.LogInformation("Anonymized {Count} old bookings", count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
// Never crash the API because of the cleanup; try again tomorrow
                logger.LogError(ex, "Anonymizing old bookings failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}