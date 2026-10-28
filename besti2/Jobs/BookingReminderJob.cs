using besti2.Application.Bookings;

namespace besti2.Jobs;

// Runs at startup and then every 15 minutes: e-mails a reminder about a day before each confirmed visit,
// and reminds businesses about new bookings that still wait for "Bekreft"
public class BookingReminderJob(IServiceScopeFactory scopeFactory, ILogger<BookingReminderJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                // A background job lives for the whole app, so it needs its own scope for the DbContext
                using var scope = scopeFactory.CreateScope();
                var reminder = scope.ServiceProvider.GetRequiredService<IBookingReminder>();
                var count = await reminder.SendRemindersAsync(DateTime.UtcNow, stoppingToken);
                if (count > 0)
                {
                    logger.LogInformation("Sent {Count} booking reminders", count);
                }

                // Businesses with new bookings that still wait for "Bekreft"
                var pending = await reminder.SendPendingRemindersAsync(DateTime.UtcNow, stoppingToken);
                if (pending > 0)
                {
                    logger.LogInformation("Reminded {Count} businesses about bookings waiting for confirmation",
                        pending);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never crash the API because of the reminders; try again in 15 minutes
                logger.LogError(ex, "Sending booking reminders failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}