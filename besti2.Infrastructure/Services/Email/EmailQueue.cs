using System.Threading.Channels;
using besti2.Application.Email;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Email;

public record QueuedEmail(string To, string Subject, string Body);

// E-mails wait here and are sent one by one in the background (EmailSenderJob), so a booking, a cancel or
// a sign-up answers at once even when the mail server is slow. Kept in memory: one API on one server.
// retryDelays = how long to wait before the 2nd, 3rd ... attempt (shorter in the tests)
public class EmailQueue(ILogger<EmailQueue> logger, TimeSpan[]? retryDelays = null)
{
    private readonly TimeSpan[] delays = retryDelays ?? [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)];

    private readonly Channel<QueuedEmail> channel =
        Channel.CreateUnbounded<QueuedEmail>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<QueuedEmail> Reader => channel.Reader;

    public void Enqueue(QueuedEmail email) => channel.Writer.TryWrite(email);

    // Sends one e-mail through the real mail server; tries again after each delay, then gives up (false)
    public async Task<bool> DeliverAsync(QueuedEmail email, IEmailService sender, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await sender.SendAsync(email.To, email.Subject, email.Body, cancellationToken);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt >= delays.Length)
                {
                    // Only the subject and the domain: the full address is personal data
                    logger.LogError(ex, "Gave up sending \"{Subject}\" to @{Domain} after {Attempts} attempts",
                        email.Subject, email.To.Split('@').LastOrDefault(), attempt + 1);
                    MailFailures.Record();
                    return false;
                }

                await Task.Delay(delays[attempt], cancellationToken);
            }
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
