using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Accounts;
using besti2.Application.Businesses;
using besti2.Application.Reviews;
using besti2.Application.Staff;
using besti2.Endpoints.Admin;
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

        // Customer clicks the link in the mail; the web page sends the id and code here.
        // Answers with the first name, so the page can say "Velkommen, Ida!"
        group.MapPost("/account/confirm-email", async (ConfirmEmailRequest request,
            ICustomerAccountService accountService, IBusinessRegistrationService registrationService,
            UserManager<AppUser> userManager) =>
        {
            var confirmed = await accountService.ConfirmEmailAsync(request);
            if (!confirmed)
            {
                return Results.BadRequest(new { message = "Lenken er ugyldig eller utløpt." });
            }

            // A business owner: the business now shows up for customers
            await registrationService.ShowBusinessOfOwnerAsync(request.UserId);
            var user = await userManager.FindByIdAsync(request.UserId);
            var firstName = user?.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return Results.Ok(new { FirstName = firstName, IsBusiness = user?.BusinessId is not null });
        }).RequireRateLimiting("signup");

        // Who is logged in: the web uses isBusiness to choose between /panel and the customer pages,
        // isAdmin for the link to /admin, isStaff for "Min arbeidsplan" (/ansatt)
        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            AdminEmails adminEmails, IStaffService staffService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new
                {
                    user.Email, user.FullName, Phone = user.PhoneNumber, IsBusiness = user.BusinessId is not null,
                    IsAdmin = adminEmails.IsAdmin(user),
                    IsStaff = user.BusinessId is null && await staffService.IsStaffAsync(user.Id, cancellationToken)
                });
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

        // Customer: a photo from the visit on the own review (PNG, JPG or WebP, max 10 MB, saved as WebP)
        group.MapPost("/me/reviews/{bookingId:guid}/photo", async (Guid bookingId, IFormFile file,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            await using var stream = file.OpenReadStream();
            var error = await reviewService.SetPhotoAsync(userId, bookingId, stream, file.Length, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("upload");

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

        // Customer: "Varsler" – reminder e-mail the day before and "Snart tid!" 2 hours before, on or off
        group.MapPut("/me/notification-settings", async (NotificationSettingsRequest request,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IProfileService profileService) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var error = await profileService.SetNotificationSettingsAsync(userId, request);
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