using besti2.Infrastructure.Services.Email;
using besti2.Infrastructure.Services;

namespace besti2.Jobs;

// Sends the e-mails from the outbox (see EmailOutbox) through the mail server (Brevo): right after a new e-mail
// is saved, and every 30 seconds for the retries. The e-mails wait in the database, so a deploy loses none
public class EmailSenderJob(
    IServiceScopeFactory scopeFactory,
    EmailOutboxSignal signal,
    ILogger<EmailSenderJob> logger)
    : BackgroundService
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
                    // A background job lives for the whole app, so it needs its own scope for the DbContext
                    using var scope = scopeFactory.CreateScope();
                    var outbox = scope.ServiceProvider.GetRequiredService<EmailOutbox>();
                    taken = await outbox.SendDueAsync(DateTime.UtcNow, stoppingToken);
                    JobHeartbeats.Ran("E-post", true);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    JobHeartbeats.Ran("E-post", false);
                    // The database is down for a moment: never crash the API, try again in 30 seconds
                    logger.LogError(ex, "Sending e-mails from the outbox failed");
                    taken = 0;
                }

                // A full batch: maybe more are waiting, so no pause
                if (taken < EmailOutbox.BatchSize)
                {
                    await signal.WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The API is stopping; the e-mails not sent yet stay in the outbox for the next start
        }
    }
}
