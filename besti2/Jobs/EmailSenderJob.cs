using besti2.Infrastructure.Services;
using besti2.Infrastructure.Services.Email;

namespace besti2.Jobs;

// Sends the e-mails from EmailQueue one by one through the mail server (Brevo)
public class EmailSenderJob(EmailQueue queue, SmtpEmailService smtp, ILogger<EmailSenderJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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