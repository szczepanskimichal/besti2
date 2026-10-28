using System.Threading.Channels;
using besti2.Application.Email;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Email;

// Attempt = how many times sending already failed
public record QueuedEmail(string To, string Subject, string Body, int Attempt = 0);

// E-mails wait here and are sent in the background (EmailSenderJob), so a booking, a cancel or
// a sign-up answers at once even when the mail server is slow. Kept in memory: one API on one server.
// retryDelays = how long a failed e-mail waits before the 2nd, 3rd ... attempt (shorter in the tests)
public class EmailQueue(ILogger<EmailQueue> logger, TimeSpan[]? retryDelays = null)
{
    // About 35 minutes in total: a short Brevo outage loses no e-mails
    private readonly TimeSpan[] delays = retryDelays ??
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)];

    private readonly Channel<QueuedEmail> channel =
        Channel.CreateUnbounded<QueuedEmail>();

    public ChannelReader<QueuedEmail> Reader => channel.Reader;

    public void Enqueue(QueuedEmail email) => channel.Writer.TryWrite(email);

    // One attempt through the real mail server. A failed e-mail goes back to the queue after its delay, so it
    // never blocks the e-mails behind it (before, one e-mail to a dead server held up all others for 70 s).
    // After the last attempt it is given up (false)
    public async Task<bool> DeliverAsync(QueuedEmail email, IEmailService sender, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(email.To, email.Subject, email.Body, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (email.Attempt >= delays.Length)
            {
                // Only the subject and the domain: the full address is personal data
                logger.LogError(ex, "Gave up sending \"{Subject}\" to @{Domain} after {Attempts} attempts",
                    email.Subject, email.To.Split('@').LastOrDefault(), email.Attempt + 1);
                MailFailures.Record();
                return false;
            }

            _ = RetryLaterAsync(email with { Attempt = email.Attempt + 1 }, delays[email.Attempt], cancellationToken);
            return false;
        }
    }

    private async Task RetryLaterAsync(QueuedEmail email, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            Enqueue(email);
        }
        catch (OperationCanceledException)
        {
            // The API is stopping; a retry still waiting is lost (the log already shows the failure)
        }
    }
}

// What the rest of the app uses as IEmailService: puts the e-mail in the queue and returns at once
public class QueuedEmailService(EmailQueue queue) : IEmailService
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        queue.Enqueue(new QueuedEmail(to, subject, body));
        return Task.CompletedTask;
    }
}
