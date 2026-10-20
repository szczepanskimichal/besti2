using besti2.Application.Accounts;
using besti2.Application.Email;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, used in the confirmation link
public class CustomerAccountService(UserManager<AppUser> userManager, IEmailService emailService, string webBaseUrl)
    : ICustomerAccountService
{
    public async Task<string?> RegisterAsync(RegisterCustomerRequest request,
        CancellationToken cancellationToken = default)

    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email)
                                                        || !request.Email.Contains('@') ||
                                                        string.IsNullOrWhiteSpace(request.Password)
                                                        || request.FullName.Length > 200 || request.Email.Length > 250)

        {
            return "Fyll ut alle feltene.";
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await userManager.FindByEmailAsync(email) is not null)

        {
            return "E-postadressen er allerede i bruk. Har du en bedriftskonto? Bruk en annen e-post for kundekonto, eller logg inn.";
        }

// Customer = account without BusinessId; must confirm the email before logging in

        var user = new AppUser { UserName = email, Email = email, FullName = request.FullName.Trim() };

        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)

        {
            return "Passordet må ha minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.";
        }

        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var link = $"{webBaseUrl}/bekreft-epost?id={Uri.EscapeDataString(user.Id)}&kode={Uri.EscapeDataString(code)}";

        var body =
            $"Hei {user.FullName}!\n\n" +
            "Takk for at du opprettet en konto hos BESTI.\n" +
            $"Klikk på lenken for å bekrefte e-postadressen din:\n{link}\n\n" +
            "Har du ikke opprettet en konto, kan du se bort fra denne e-posten.";

        await emailService.SendAsync(email, "Bekreft e-postadressen din hos BESTI", body, cancellationToken);

        return null;
    }

    public async Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request)

    {
        var user = await userManager.FindByIdAsync(request.UserId);

        if (user is null)

        {
            return false;
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Code);

        return result.Succeeded;
    }
}