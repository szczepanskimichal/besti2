using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Accounts;
using besti2.Application.Businesses;
using besti2.Application.Reviews;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // Customer sign-up: creates the account and sends a confirmation mail
        group.MapPost("/account/register", async (RegisterCustomerRequest request,
            ICustomerAccountService accountService, CancellationToken cancellationToken) =>
        {
            var error = await accountService.RegisterAsync(request, cancellationToken);
            return error is null ? Results.Created() : Results.BadRequest(new { message = error });
        }).RequireRateLimiting("signup");

        // Customer clicks the link in the mail; the web page sends the id and code here
        group.MapPost("/account/confirm-email", async (ConfirmEmailRequest request,
            ICustomerAccountService accountService, IBusinessRegistrationService registrationService) =>
        {
            var confirmed = await accountService.ConfirmEmailAsync(request);
            if (confirmed)
            {
                // A business owner: the business now shows up for customers
                await registrationService.ShowBusinessOfOwnerAsync(request.UserId);
            }

            return confirmed
                ? Results.NoContent()
                : Results.BadRequest(new { message = "Lenken er ugyldig eller utløpt." });
        }).RequireRateLimiting("signup");

        // Who is logged in: the web uses isBusiness to choose between /panel and the customer pages
        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new
                    { user.Email, user.FullName, Phone = user.PhoneNumber, IsBusiness = user.BusinessId is not null });
        }).RequireAuthorization();

        // Customer: review a past booking
        group.MapPost("/me/reviews", async (CreateReviewRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IReviewService reviewService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var error = await reviewService.CreateAsync(userId, request, cancellationToken);
            return error is null ? Results.Created() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Customer: "Min profil" – name, e-mail (read only) and phone
        group.MapGet("/me/profile", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IProfileService profileService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            var profile = userId is null ? null : await profileService.GetAsync(userId, cancellationToken);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        }).RequireAuthorization();
        // Customer: "Last ned mine data" – everything BESTI keeps about the customer, as JSON
        group.MapGet("/me/export", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IProfileService profileService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            var export = userId is null ? null : await profileService.ExportAsync(userId, cancellationToken);
            return export is null ? Results.NotFound() : Results.Ok(export);
        }).RequireAuthorization();

        group.MapPut("/me/profile", async (UpdateMyProfileRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IProfileService profileService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var error = await profileService.UpdateAsync(userId, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        group.MapPost("/me/password", async (ChangePasswordRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IProfileService profileService) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var error = await profileService.ChangePasswordAsync(userId, request);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization().RequireRateLimiting("login");

        // Customer: "Slett konto" (confirmed with the password)
        group.MapPost("/me/delete", async (DeleteAccountRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IProfileService profileService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var error = await profileService.DeleteAsync(userId, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization().RequireRateLimiting("login");
    }
}