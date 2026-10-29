using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints.Admin;

// Who may open /admin: the e-mails in Admin:Emails (user-secrets locally, Admin__Emails in /opt/besti/.env),
// separated by commas. No button anywhere makes someone an admin; changing the list needs the server
public sealed class AdminEmails(IConfiguration configuration)
{
    public const string Policy = "Admin";

    private readonly HashSet<string> emails = (configuration["Admin:Emails"] ?? "")
        .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public bool IsAdmin(AppUser? user) =>
        user is { EmailConfirmed: true, BusinessId: null, Email: not null } && emails.Contains(user.Email);
}

public sealed class AdminRequirement : IAuthorizationRequirement;

// A business owner account is never an admin (one login, one role)
public sealed class AdminHandler(UserManager<AppUser> userManager, AdminEmails adminEmails)
    : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && adminEmails.IsAdmin(await userManager.GetUserAsync(context.User)))
        {
            context.Succeed(requirement);
        }
    }
}
