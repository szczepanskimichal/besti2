using besti2.Application.Bookings;
using besti2.Application.Invites;
using besti2.Infrastructure.Services.Email;
using besti2.Infrastructure.Services;

namespace besti2.Jobs;

// Runs at startup and then every 15 minutes: e-mails a reminder about a day before each confirmed visit,
// reminds businesses about new bookings that still wait for "Bekreft", and closes BESTI sammen groups whose
// answer time is up (the business gets the final count).
// After each run it pings healthchecks.io (Monitoring__MailPingUrl), with /fail when e-mails could not be sent
public class BookingReminderJob(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<BookingReminderJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            var ok = true;
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

                // "Snart tid!" in the customers' bells and on their phones
                await reminder.SendSoonNoticesAsync(DateTime.UtcNow, stoppingToken);

                // Businesses with new bookings that still wait for "Bekreft"
                var pending = await reminder.SendPendingRemindersAsync(DateTime.UtcNow, stoppingToken);
                if (pending > 0)
                {
                    logger.LogInformation("Reminded {Count} businesses about bookings waiting for confirmation",
                        pending);
                }

                // BESTI sammen: groups nobody has opened since the answer time ran out
                var closed = await scope.ServiceProvider.GetRequiredService<IInviteService>()
                    .CloseDueAsync(DateTime.UtcNow, stoppingToken);
                if (closed > 0)
                {
                    logger.LogInformation("Closed {Count} BESTI sammen invitations", closed);
                }

                JobHeartbeats.Ran("Påminnelser", true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                JobHeartbeats.Ran("Påminnelser", false);
                // Never crash the API because of the reminders; try again in 15 minutes
                logger.LogError(ex, "Sending booking reminders failed");
                ok = false;
            }

            await PingAsync(ok, stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PingAsync(bool ok, CancellationToken stoppingToken)
    {
        var failures = MailFailures.Take();
        var url = configuration["Monitoring:MailPingUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            return; // not set up (locally and in the tests)
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var target = ok && failures == 0 ? url : $"{url.TrimEnd('/')}/fail";
            await client.PostAsync(target, new StringContent($"Mail failures: {failures}"), stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // healthchecks.io is down or unreachable: it alerts by itself when the pings stop
            logger.LogWarning(ex, "Could not ping the mail monitoring");
        }
    }
}
