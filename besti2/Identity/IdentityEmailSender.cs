using besti2.Application.Email;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Identity;

// Sends Identity's password reset mail through our own email service, in Norwegian
public class IdentityEmailSender(IEmailService emailService, IConfiguration configuration) : IEmailSender<AppUser>
{
    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        var webUrl = configuration["WebBaseUrl"] ?? "http://localhost:3000";
        var link =
            $"{webUrl}/glemt-passord/ny?epost={Uri.EscapeDataString(email)}&kode={Uri.EscapeDataString(resetCode)}";
        var body =
            "Hei!\n\n" +
            "Vi har fått en forespørsel om nytt passord for kontoen din hos BESTI.\n" +
            $"Klikk på lenken for å velge et nytt passord:\n{link}\n\n" +
            "Har du ikke bedt om dette, kan du se bort fra denne e-posten.";
        return emailService.SendAsync(email, "Nytt passord hos BESTI", body);
    }

    // Not used yet (email confirmation comes with customer accounts)
    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) => Task.CompletedTask;
}