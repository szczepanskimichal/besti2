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
        var smtp = new FlakyEmailService(failures: 2);

        var delivered = await NewQueue().DeliverAsync(new QueuedEmail("kari@test.example", "Hei", "Hei"), smtp,
            CancellationToken.None);

        Assert.True(delivered);
        Assert.Equal(3, smtp.Attempts);
    }

    // check if a mail server that is down is given up after 3 attempts (the next e-mails must not wait forever)
    [Fact]
    public async Task DeliverAsync_ServerDown_GivesUpAfterThreeAttempts()
    {
        var smtp = new FlakyEmailService(failures: int.MaxValue);

        var delivered = await NewQueue().DeliverAsync(new QueuedEmail("kari@test.example", "Hei", "Hei"), smtp,
            CancellationToken.None);

        Assert.False(delivered);
        Assert.Equal(3, smtp.Attempts);
    }
}
