using System.Threading.Channels;
using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Email;

// E-mails wait in the database (table OutboxEmails) and are sent in the background (EmailSenderJob), so a booking,
// a cancel or a sign-up answers at once even when the mail server is slow, and a deploy or a crash loses no e-mails.
// retryDelays = how long a failed e-mail waits before the 2nd, 3rd ... attempt (shorter in the tests)
public class EmailOutbox(
    AppDbContext db,
    IEmailService sender,
    ILogger<EmailOutbox> logger,
    TimeSpan[]? retryDelays = null)
{
    // About 35 minutes in total: a short Brevo outage loses no e-mails
    private readonly TimeSpan[] delays = retryDelays ??
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)];

    // How many e-mails one round takes, and how many of them are sent at the same time: one mail takes about
    // a second, so one by one a busy hour (or 50 bookings at once) made the last mail wait many minutes
    public const int BatchSize = 20;
    public const int Senders = 4;

    // The time a round has to send the e-mails it took; after that another round (or the new API after a crash)
    // takes them again. Far longer than the mail server timeouts
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);

    // Sends the e-mails that are due, at most BatchSize. Returns how many were taken (0 = nothing to do now)
    public async Task<int> SendDueAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var emails = await db.OutboxEmails
            .Where(e => e.NextAttemptUtc <= nowUtc)
            .OrderBy(e => e.NextAttemptUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
        if (emails.Count == 0)
        {
            return 0;
        }

        // Take them first, so nobody else sends them at the same time
        foreach (var email in emails)
        {
            email.NextAttemptUtc = nowUtc + Lease;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another API took some of them a moment earlier; the next round tries again
            db.ChangeTracker.Clear();
            return 0;
        }

        // Only the mail server in parallel; the DbContext is used again after all sends are done
        var results = new Dictionary<OutboxEmail, Exception?>();
        await Parallel.ForEachAsync(emails,
            new ParallelOptions { MaxDegreeOfParallelism = Senders, CancellationToken = CancellationToken.None },
            async (email, _) =>
            {
                Exception? error = null;
                try
                {
                    await sender.SendAsync(email.To, email.Subject, email.Body, cancellationToken);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                lock (results)
                {
                    results[email] = error;
                }
            });

        foreach (var (email, error) in results)
        {
            if (error is null)
            {
                db.OutboxEmails.Remove(email);
            }
            else if (error is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                // The API is stopping: the e-mail goes out right after the restart, not 10 minutes later
                email.NextAttemptUtc = nowUtc;
            }
            else if (email.Attempts >= delays.Length)
            {
                // Only the subject and the domain: the full address is personal data
                logger.LogError(error, "Gave up sending \"{Subject}\" to @{Domain} after {Attempts} attempts",
                    email.Subject, email.To.Split('@').LastOrDefault(), email.Attempts + 1);
                MailFailures.Record();
                db.OutboxEmails.Remove(email);
            }
            else
            {
                logger.LogWarning(error, "Sending \"{Subject}\" failed, trying again in {Delay}",
                    email.Subject, delays[email.Attempts]);
                email.NextAttemptUtc = nowUtc + delays[email.Attempts];
                email.Attempts++;
            }
        }

        try
        {
            // Not cancelled: what was sent must be saved as sent, also when the API is stopping
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // The lease ran out and another API took them; it sends them again (at worst one e-mail twice)
            logger.LogWarning(ex, "E-mails were taken by another sender before they were saved as sent");
            db.ChangeTracker.Clear();
        }

        return emails.Count;
    }
}

// What the rest of the app uses as IEmailService: saves the e-mail in the outbox and returns at once.
// Its own DbContext, so it never saves half-done changes of the service that sends the e-mail
public class OutboxEmailService(IServiceScopeFactory scopeFactory, EmailOutboxSignal signal) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            db.OutboxEmails.Add(new OutboxEmail
            {
                Id = Guid.NewGuid(),
                To = to,
                Subject = subject,
                Body = body,
                CreatedUtc = now,
                NextAttemptUtc = now
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        signal.Notify();
    }
}

// Wakes EmailSenderJob as soon as a new e-mail is saved, so it does not wait for its next check
public class EmailOutboxSignal
{
    private readonly Channel<bool> channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => channel.Writer.TryWrite(true);

    // Waits for a new e-mail, at most maxWait
    public async Task WaitAsync(TimeSpan maxWait, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(maxWait);
        try
        {
            await channel.Reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // maxWait has passed: check the outbox anyway (retries that are due now)
        }
    }
}
