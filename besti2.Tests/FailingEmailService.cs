using besti2.Application.Email;

namespace besti2.Tests;

// A mail server that is down: every send throws
public class FailingEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Mail server is down");
}