using besti2.Application.Accounts;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Infrastructure.Services;

public class GoogleLoginService(UserManager<AppUser> userManager, IGoogleTokenValidator validator)
    : IGoogleLoginService
{
    public const string Provider = "Google";
    private const string Failed = "Kunne ikke logge inn med Google. Prøv igjen.";

    public async Task<GoogleLoginResult> FindOrCreateUserAsync(string idToken,
        CancellationToken cancellationToken = default)
    {
        var google = await validator.ValidateAsync(idToken, cancellationToken);
        if (google is null)
        {
            return new GoogleLoginResult(null, Failed);
        }

        // Logged in with this Google account before (also after a new e-mail at Google)
        var user = await userManager.FindByLoginAsync(Provider, google.Subject);
        if (user is not null)
        {
            return await CheckLockoutAsync(user);
        }

        // Google has not checked the e-mail: it could belong to someone else
        if (!google.EmailVerified)
        {
            return new GoogleLoginResult(null, "Bekreft e-postadressen hos Google først, eller logg inn med passord.");
        }

        var email = google.Email.Trim().ToLowerInvariant();
        user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return await CreateCustomerAsync(email, google);
        }

        if (!user.EmailConfirmed)
        {
            // A business must finish its own sign-up (the confirmation starts BESTI's approval)
            if (user.BusinessId is not null)
            {
                return new GoogleLoginResult(null, "Bekreft e-posten fra registreringen av bedriften først.");
            }

            // Google has shown that this person owns the e-mail. A password from an unconfirmed sign-up
            // could be a stranger's, so it is removed (the owner can make one under Profil)
            if (await userManager.HasPasswordAsync(user)
                && !(await userManager.RemovePasswordAsync(user)).Succeeded)
            {
                return new GoogleLoginResult(null, Failed);
            }

            user.EmailConfirmed = true;
            if (!(await userManager.UpdateAsync(user)).Succeeded)
            {
                return new GoogleLoginResult(null, Failed);
            }
        }

        // The same person: from now on the Google account is linked to this account
        if (!(await userManager.AddLoginAsync(user, new UserLoginInfo(Provider, google.Subject, Provider))).Succeeded)
        {
            return new GoogleLoginResult(null, Failed);
        }

        return await CheckLockoutAsync(user);
    }

    // New customer account without a password, e-mail already confirmed by Google
    private async Task<GoogleLoginResult> CreateCustomerAsync(string email, GoogleIdentity google)
    {
        var name = string.IsNullOrWhiteSpace(google.Name) ? email.Split('@')[0] : google.Name.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = name.Length > 100 ? name[..100] : name
        };
        if (!(await userManager.CreateAsync(user)).Succeeded
            || !(await userManager.AddLoginAsync(user, new UserLoginInfo(Provider, google.Subject, Provider)))
                .Succeeded)
        {
            return new GoogleLoginResult(null, Failed);
        }

        return new GoogleLoginResult(user.Id, null);
    }

    // Too many wrong passwords lock the account for a while, also for Google
    private async Task<GoogleLoginResult> CheckLockoutAsync(AppUser user) =>
        await userManager.IsLockedOutAsync(user)
            ? new GoogleLoginResult(null, "For mange feil passord. Vent 5 minutter og prøv igjen.")
            : new GoogleLoginResult(user.Id, null);
}
