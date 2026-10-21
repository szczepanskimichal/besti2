using besti2.Application.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace besti2.Infrastructure.Services;

public class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(configuration["Smtp:From"]!));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        // Max 10 s per step: a slow mail server must not keep a booking "loading" for 2 minutes
        using var client = new SmtpClient { Timeout = 10_000 };
        // CancellationToken.None: the booking is already saved, so the mail is sent
        // even if the customer closes the page in the meantime
        await client.ConnectAsync(configuration["Smtp:Host"]!, configuration.GetValue<int>("Smtp:Port"),
            SecureSocketOptions.Auto, CancellationToken.None);
        // Real mail servers (Brevo) need a login; Mailpit on this machine does not
        var username = configuration["Smtp:Username"];
        if (!string.IsNullOrEmpty(username))
        {
            await client.AuthenticateAsync(username, configuration["Smtp:Password"] ?? "", CancellationToken.None);
        }

        await client.SendAsync(message, CancellationToken.None);
        await client.DisconnectAsync(true, CancellationToken.None);
    }
}