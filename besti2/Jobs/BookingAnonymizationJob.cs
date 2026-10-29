using besti2.Application.Bookings;
using besti2.Application.Notifications;
using besti2.Infrastructure.Services;

namespace besti2.Jobs;

// Runs at startup and then once a day: anonymizes bookings older than 3 years and deletes old notifications
// (7 days customer, 14 days business) – the periods promised on /personvern
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
                    logger.LogInformation("Anonymized {Count} old bookings and reviews", count);
                    
                }

                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var deleted = await notifications.DeleteExpiredAsync(DateTime.UtcNow, stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Deleted {Count} old notifications", deleted);
                }

                JobHeartbeats.Ran("Anonymisering", true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                JobHeartbeats.Ran("Anonymisering", false);
                // Never crash the API because of the cleanup; try again tomorrow
                logger.LogError(ex, "Anonymizing old bookings failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}