using besti2.Application.Email;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace besti2.Identity;

// Sends Identity's password reset mail through our own email service, in Norwegian
public class IdentityEmailSender(IEmailService emailService, IConfiguration configuration,
    ILogger<IdentityEmailSender> logger, MailLimiter mailLimiter) : IEmailSender<AppUser>
{
    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        // Same guard as the sign-up mails; the answer to the form stays the same, so nobody learns anything
        if (!mailLimiter.TryReserve(email))
        {
            logger.LogWarning("Password reset mail to user {UserId} skipped: one was sent less than 5 minutes ago, or today's limit is used up", user.Id);
            return;
        }
        var webUrl = configuration["WebBaseUrl"] ?? "http://localhost:3000";
        var link =
            $"{webUrl}/glemt-passord/ny?epost={Uri.EscapeDataString(email)}&kode={Uri.EscapeDataString(resetCode)}";
        var body =
            "Hei!\n\n" +
            "Vi har fått en forespørsel om nytt passord for kontoen din hos BESTI.\n" +
            $"Klikk på lenken for å velge et nytt passord:\n{link}\n\n" +
            "Har du ikke bedt om dette, kan du se bort fra denne e-posten.";
        // A failed mail must not give an error page: the answer has to look the same whether the
        // account exists or not (nobody can find out who has an account)
        try
        {
            await emailService.SendAsync(email, "Nytt passord hos BESTI", body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send the password reset email to user {UserId}", user.Id);
        }
    }

    // Not used yet (email confirmation comes with customer accounts)
    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) => Task.CompletedTask;
}