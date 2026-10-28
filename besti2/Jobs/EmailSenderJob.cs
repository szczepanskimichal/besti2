using besti2.Infrastructure.Services;
using besti2.Infrastructure.Services.Email;

namespace besti2.Jobs;

// Sends the e-mails from EmailQueue through the mail server (Brevo), 4 at a time: one mail takes about
// a second, so one by one a busy hour (or 50 bookings at once) made the last mail wait many minutes
public class EmailSenderJob(EmailQueue queue, SmtpEmailService smtp, ILogger<EmailSenderJob> logger)
    : BackgroundService
{
    public const int Senders = 4;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Enumerable.Range(0, Senders).Select(_ => SendLoopAsync(stoppingToken)));

    private async Task SendLoopAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var email in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await queue.DeliverAsync(email, smtp, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The API is stopping: StopAsync sends what is left
        }
    }

    // A deploy restarts the API: the e-mails still waiting get one quick try first (Docker waits 10 s)
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!limit.IsCancellationRequested && queue.Reader.TryRead(out var email))
        {
            try
            {
                await smtp.SendAsync(email.To, email.Subject, email.Body, limit.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "E-mail \"{Subject}\" not sent before the API stopped", email.Subject);
            }
        }
    }
}