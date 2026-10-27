using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints.Panel;

// The logged-in owner's business. Every /api/panel endpoint takes it as a parameter instead of
// looking up the user itself; the BusinessOwner policy has already checked the account (403 for a customer)
public sealed record PanelBusiness(Guid Id)
{
    public const string Policy = "BusinessOwner";

    public static ValueTask<PanelBusiness?> BindAsync(HttpContext context) =>
        ValueTask.FromResult(context.Items[nameof(PanelBusiness)] is Guid id ? new PanelBusiness(id) : null);
}

public sealed class BusinessOwnerRequirement : IAuthorizationRequirement;

// Lets only an account with a business through and remembers that business for PanelBusiness
public sealed class BusinessOwnerHandler(UserManager<AppUser> userManager)
    : AuthorizationHandler<BusinessOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        BusinessOwnerRequirement requirement)
    {
        if (context.Resource is not HttpContext http || context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await userManager.GetUserAsync(context.User);
        if (user?.BusinessId is Guid businessId)
        {
            http.Items[nameof(PanelBusiness)] = businessId;
            context.Succeed(requirement);
        }
    }
}