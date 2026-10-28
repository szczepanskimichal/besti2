using besti2.Application.Email;
using besti2.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class EmailQueueTests
{
    // No real waiting between attempts in the tests
    private static EmailQueue NewQueue() =>
        new(NullLogger<EmailQueue>.Instance, [TimeSpan.Zero, TimeSpan.Zero]);

    // A mail server that fails the first times, then works
    private class FlakyEmailService(int failures) : IEmailService
    {
        public int Attempts { get; private set; }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            Attempts++;
            return Attempts <= failures
                ? throw new InvalidOperationException("Mail server is slow")
                : Task.CompletedTask;
        }
    }

    // check if an e-mail "sent" by the app waits in the queue and then reaches the mail server
    [Fact]
    public async Task QueuedEmailService_PutsTheEmailInTheQueue()
    {
        var queue = NewQueue();
        await new QueuedEmailService(queue).SendAsync("kari@test.example", "Bestilling mottatt", "Hei");

        Assert.True(queue.Reader.TryRead(out var email));
        var smtp = new FakeEmailService();
        Assert.True(await queue.DeliverAsync(email!, smtp, CancellationToken.None));
        var sent = Assert.Single(smtp.Sent);
        Assert.Equal("kari@test.example", sent.To);
        Assert.Equal("Bestilling mottatt", sent.Subject);
    }

    // check if a mail server that fails twice still gets the e-mail on the third attempt
    [Fact]
    public async Task DeliverAsync_FailsTwice_SentOnThirdAttempt()
    {
        var queue = NewQueue();
        var smtp = new FlakyEmailService(failures: 2);
        queue.Enqueue(new QueuedEmail("kari@test.example", "Hei", "Hei"));

        var delivered = false;
        for (var i = 0; i < 3; i++)
        {
            // A failed e-mail comes back to the queue after its delay
            var email = await queue.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            delivered = await queue.DeliverAsync(email, smtp, CancellationToken.None);
        }

        Assert.True(delivered);
        Assert.Equal(3, smtp.Attempts);
    }

    // check if a failed e-mail does not block the next one: the next one is sent before the retry
    [Fact]
    public async Task DeliverAsync_FailedEmail_DoesNotBlockTheNext()
    {
        var queue = new EmailQueue(NullLogger<EmailQueue>.Instance, [TimeSpan.FromMinutes(10)]);
        var smtp = new FlakyEmailService(failures: 1);

        var first = await queue.DeliverAsync(new QueuedEmail("kari@test.example", "Første", "Hei"), smtp,
            CancellationToken.None);
        var second = await queue.DeliverAsync(new QueuedEmail("ola@test.example", "Andre", "Hei"), smtp,
            CancellationToken.None);

        Assert.False(first);
        Assert.True(second);
        Assert.False(queue.Reader.TryRead(out _)); // the retry of the first one waits 10 minutes
    }

    // check if a mail server that is down is given up after 3 attempts (the queue must not grow forever)
    [Fact]
    public async Task DeliverAsync_ServerDown_GivesUpAfterThreeAttempts()
    {
        var queue = NewQueue();
        var smtp = new FlakyEmailService(failures: int.MaxValue);
        queue.Enqueue(new QueuedEmail("kari@test.example", "Hei", "Hei"));

        for (var i = 0; i < 3; i++)
        {
            var email = await queue.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(await queue.DeliverAsync(email, smtp, CancellationToken.None));
        }

        await Task.Delay(100);
        Assert.False(queue.Reader.TryRead(out _));
        Assert.Equal(3, smtp.Attempts);
    }
}
