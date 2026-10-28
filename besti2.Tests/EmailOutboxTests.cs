using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class EmailOutboxTests
{
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);

    // A mail server that fails the first times, then works
    private class FlakyEmailService(int failures) : IEmailService
    {
        private int attempts;
        public int Attempts => attempts;

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            return Interlocked.Increment(ref attempts) <= failures
                ? throw new InvalidOperationException("Mail server is slow")
                : Task.CompletedTask;
        }
    }

    // One in-memory database, like the API: every scope gets its own DbContext on it
    private static ServiceProvider NewServices()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<EmailOutboxSignal>();
        return services.BuildServiceProvider();
    }

    private static AppDbContext NewDb(ServiceProvider services) =>
        services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    // 1, 5 and 30 minutes, like in the API
    private static EmailOutbox NewOutbox(ServiceProvider services, IEmailService smtp) =>
        new(NewDb(services), smtp, NullLogger<EmailOutbox>.Instance);

    private static OutboxEmail NewEmail(string to = "kari@test.example", int attempts = 0) => new()
    {
        Id = Guid.NewGuid(),
        To = to,
        Subject = "Bestilling mottatt",
        Body = "Hei",
        CreatedUtc = Now,
        Attempts = attempts,
        NextAttemptUtc = Now
    };

    private static async Task AddAsync(ServiceProvider services, params OutboxEmail[] emails)
    {
        await using var db = NewDb(services);
        db.OutboxEmails.AddRange(emails);
        await db.SaveChangesAsync();
    }

    // check if an e-mail "sent" by the app is saved in the database and then reaches the mail server
    [Fact]
    public async Task OutboxEmailService_SavesTheEmail_ThenItIsSent()
    {
        await using var services = NewServices();
        var app = new OutboxEmailService(services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<EmailOutboxSignal>());

        await app.SendAsync("kari@test.example", "Bestilling mottatt", "Hei");

        await using (var db = NewDb(services))
        {
            var saved = await db.OutboxEmails.SingleAsync();
            Assert.Equal("kari@test.example", saved.To);
            Assert.Equal(0, saved.Attempts);
        }

        var smtp = new FakeEmailService();
        Assert.Equal(1, await NewOutbox(services, smtp).SendDueAsync(DateTime.UtcNow.AddSeconds(1), CancellationToken.None));
        var sent = Assert.Single(smtp.Sent);
        Assert.Equal("Bestilling mottatt", sent.Subject);
        await using (var db = NewDb(services))
        {
            Assert.Empty(db.OutboxEmails); // a sent e-mail is not kept (personal data)
        }
    }

    // check if a failed e-mail waits 1 minute and is not sent again before that
    [Fact]
    public async Task SendDueAsync_Fails_WaitsOneMinuteBeforeTheRetry()
    {
        await using var services = NewServices();
        await AddAsync(services, NewEmail());
        var smtp = new FlakyEmailService(failures: 1);

        await NewOutbox(services, smtp).SendDueAsync(Now, CancellationToken.None);
        Assert.Equal(0, await NewOutbox(services, smtp).SendDueAsync(Now.AddSeconds(59), CancellationToken.None));

        await using var db = NewDb(services);
        var email = await db.OutboxEmails.SingleAsync();
        Assert.Equal(1, email.Attempts);
        Assert.Equal(Now.AddMinutes(1), email.NextAttemptUtc);
        Assert.Equal(1, smtp.Attempts);
    }

    // check if a mail server that fails twice still gets the e-mail on the third attempt
    [Fact]
    public async Task SendDueAsync_FailsTwice_SentOnThirdAttempt()
    {
        await using var services = NewServices();
        await AddAsync(services, NewEmail());
        var smtp = new FlakyEmailService(failures: 2);

        await NewOutbox(services, smtp).SendDueAsync(Now, CancellationToken.None);
        await NewOutbox(services, smtp).SendDueAsync(Now.AddMinutes(1), CancellationToken.None);
        await NewOutbox(services, smtp).SendDueAsync(Now.AddMinutes(6), CancellationToken.None);

        Assert.Equal(3, smtp.Attempts);
        await using var db = NewDb(services);
        Assert.Empty(db.OutboxEmails);
    }

    // check if a mail server that is down is given up after 4 attempts (the outbox must not grow forever)
    [Fact]
    public async Task SendDueAsync_ServerDown_GivesUpAfterFourAttempts()
    {
        await using var services = NewServices();
        await AddAsync(services, NewEmail());
        var smtp = new FlakyEmailService(failures: int.MaxValue);

        var time = Now;
        foreach (var wait in new[] { 0, 1, 5, 30 })
        {
            time = time.AddMinutes(wait);
            Assert.Equal(1, await NewOutbox(services, smtp).SendDueAsync(time, CancellationToken.None));
        }

        Assert.Equal(4, smtp.Attempts);
        await using var db = NewDb(services);
        Assert.Empty(db.OutboxEmails);
    }

    // check if a failed e-mail does not block the next one
    [Fact]
    public async Task SendDueAsync_FailedEmail_DoesNotBlockTheNext()
    {
        await using var services = NewServices();
        await AddAsync(services, NewEmail("kari@test.example"));
        var smtp = new FlakyEmailService(failures: 1);
        await NewOutbox(services, smtp).SendDueAsync(Now, CancellationToken.None);

        await AddAsync(services, NewEmail("ola@test.example"));
        Assert.Equal(1, await NewOutbox(services, smtp).SendDueAsync(Now.AddSeconds(10), CancellationToken.None));

        await using var db = NewDb(services);
        var waiting = await db.OutboxEmails.SingleAsync(); // only the first one, waiting for its retry
        Assert.Equal("kari@test.example", waiting.To);
    }

    // check if e-mails saved before a restart (a deploy) are sent by the new API
    [Fact]
    public async Task SendDueAsync_AfterRestart_SendsWhatWasWaiting()
    {
        await using var services = NewServices();
        // One waiting for its retry, one taken by the old API that stopped before it was sent
        var retry = NewEmail("kari@test.example", attempts: 1);
        retry.NextAttemptUtc = Now.AddMinutes(4);
        var taken = NewEmail("ola@test.example");
        taken.NextAttemptUtc = Now.Add(EmailOutbox.Lease);
        await AddAsync(services, retry, taken);

        var smtp = new FakeEmailService();
        Assert.Equal(1, await NewOutbox(services, smtp).SendDueAsync(Now.AddMinutes(5), CancellationToken.None));
        Assert.Equal(1, await NewOutbox(services, smtp).SendDueAsync(Now.AddMinutes(11), CancellationToken.None));

        Assert.Equal(2, smtp.Sent.Count);
    }

    // check if one round sends at most one batch, so a big pile does not hold one round for long
    [Fact]
    public async Task SendDueAsync_ManyEmails_OneBatchPerRound()
    {
        await using var services = NewServices();
        await AddAsync(services, Enumerable.Range(0, 25).Select(i => NewEmail($"kunde{i}@test.example")).ToArray());
        var smtp = new FakeEmailService();

        Assert.Equal(EmailOutbox.BatchSize, await NewOutbox(services, smtp).SendDueAsync(Now, CancellationToken.None));
        Assert.Equal(5, await NewOutbox(services, smtp).SendDueAsync(Now, CancellationToken.None));
        Assert.Equal(0, await NewOutbox(services, smtp).SendDueAsync(Now, CancellationToken.None));
        Assert.Equal(25, smtp.Sent.Count);
    }
}
