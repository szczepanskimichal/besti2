using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Bookings;
using besti2.Application.Businesses;
using besti2.Application.Images;
using besti2.Application.Reviews;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints;

public static class PanelEndpoints
{
    public static void MapPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/panel");

        // Business panel: the owner's reviews
        group.MapGet("/reviews", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IReviewService reviewService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);

            return user?.BusinessId is Guid businessId
                ? Results.Ok(await reviewService.GetForBusinessAsync(businessId, cancellationToken))
                : Results.Forbid();
        }).RequireAuthorization();
        // Business panel: reply to a review (cannot delete it)
        group.MapPost("/reviews/{id:guid}/reply", async (Guid id, ReplyToReviewRequest request,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }
            var error = await reviewService.ReplyAsync(businessId, id, request.Reply, cancellationToken);

            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // ?from=2026-10-20&to=2026-10-26 = the days shown in the calendar (Oslo time); without: the list
        group.MapGet("/bookings", async (DateOnly? from, DateOnly? to, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            // A calendar shows at most about two months at once
            if (from is DateOnly first && to is DateOnly last && (last < first || last.DayNumber - first.DayNumber > 62))
            {
                return Results.BadRequest(new { message = "Ugyldig periode." });
            }

            return Results.Ok(await bookingService.GetForBusinessAsync(businessId, from, to, cancellationToken));
        }).RequireAuthorization();

        // Business panel: the business adds a visit itself (phone call, walk-in); confirmed at once
        group.MapPost("/bookings", async (CreateManualBookingRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var result = await bookingService.CreateByBusinessAsync(businessId, request, cancellationToken);
            return result.Outcome switch
            {
                CreateBookingOutcome.Created => Results.Created($"/api/panel/bookings/{result.Booking!.Id}", result.Booking),
                CreateBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
                CreateBookingOutcome.SlotTaken => Results.Conflict(
                    new { message = "Den ansatte har allerede en time da. Velg et annet tidspunkt." }),
                _ => Results.NotFound()
            };
        }).RequireAuthorization();
        // Business panel: blocked time (holiday, doctor, closed day) for the calendar days from-to
        group.MapGet("/timeoff", async (DateOnly from, DateOnly to, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, ITimeOffService timeOffService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            if (to < from || to.DayNumber - from.DayNumber > 62)
            {
                return Results.BadRequest(new { message = "Ugyldig periode." });
            }

            return Results.Ok(await timeOffService.GetForBusinessAsync(businessId, from, to, cancellationToken));
        }).RequireAuthorization();

        // Business panel: block time for one employee or the whole business (visits already there are kept)
        group.MapPost("/timeoff", async (CreateTimeOffRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, ITimeOffService timeOffService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var result = await timeOffService.CreateAsync(businessId, request, cancellationToken);
            if (result is null)
            {
                return Results.NotFound();
            }

            return result.Error is null
                ? Results.Created($"/api/panel/timeoff/{result.TimeOff!.Id}", result)
                : Results.BadRequest(new { message = result.Error });
        }).RequireAuthorization();

        // Business panel: remove blocked time
        group.MapDelete("/timeoff/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, ITimeOffService timeOffService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            return await timeOffService.DeleteAsync(businessId, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }).RequireAuthorization();

        // Business panel: the owner's own business with services and employees
        group.MapGet("/business", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var business = await businessService.GetForPanelAsync(businessId, cancellationToken);
            return business is null ? Results.NotFound() : Results.Ok(business);
        }).RequireAuthorization();

        // Business panel: add a service to the owner's business
        group.MapPost("/services", async (CreateServiceRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.AddServiceAsync(businessId, request, cancellationToken);
            return error is null ? Results.Created() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: edit one of the owner's services
        group.MapPut("/services/{id:guid}", async (Guid id, CreateServiceRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.UpdateServiceAsync(businessId, id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: soft delete one of the owner's services
        group.MapDelete("/services/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var deleted = await businessService.DeleteServiceAsync(businessId, id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        // Business panel: add an employee
        group.MapPost("/employees", async (EmployeeRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);

            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.AddEmployeeAsync(businessId, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: edit one of the owner's employees
        group.MapPut("/employees/{id:guid}", async (Guid id, EmployeeRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.UpdateEmployeeAsync(businessId, id, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: soft delete one of the owner's employees (never the last one)
        group.MapDelete("/employees/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.DeleteEmployeeAsync(businessId, id, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: owner edits description and contact info
        group.MapPut("/business", async (UpdateProfileRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.UpdateProfileAsync(businessId, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: owner updates opening hours
        group.MapPut("/opening-hours", async (UpdateOpeningHoursRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessService businessService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var error = await businessService.UpdateOpeningHoursAsync(businessId, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Business panel: owner uploads a logo (PNG, JPG or WebP, max 10 MB, saved as small WebP)
        group.MapPost("/logo", async (IFormFile file, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessImageService imageService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            await using var stream = file.OpenReadStream();
            var error = await imageService.SetLogoAsync(businessId, stream, file.Length, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("upload");

        // Business panel: add a photo to the gallery
        group.MapPost("/gallery", async (IFormFile file, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessImageService imageService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            await using var stream = file.OpenReadStream();
            var error = await imageService.AddGalleryImageAsync(businessId, stream, file.Length, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("upload");
        // Business panel: delete a gallery photo
        group.MapDelete("/gallery/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBusinessImageService imageService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            return await imageService.DeleteGalleryImageAsync(businessId, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }).RequireAuthorization();

        // Confirm a booking
        group.MapPost("/bookings/{id:guid}/bekreft", (Guid id, ClaimsPrincipal principal,
                    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
                ChangeBookingStatusAsync(id, BookingStatus.Bekreftet, null, principal, userManager, bookingService,
                    cancellationToken))
            .RequireAuthorization();

        // Cancel a booking; the body { message } is optional
        group.MapPost("/bookings/{id:guid}/avlys", (Guid id, CancelBookingRequest? request, ClaimsPrincipal principal,
                UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
            ChangeBookingStatusAsync(id, BookingStatus.Avlyst, request?.Message, principal, userManager, bookingService,
                cancellationToken))
            .RequireAuthorization();

        // Move a visit to another time and/or employee
        // The customer did not come (only after the start time)
        group.MapPost("/bookings/{id:guid}/motte-ikke", (Guid id, ClaimsPrincipal principal,
                    UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
                ChangeBookingStatusAsync(id, BookingStatus.MotteIkke, null, principal, userManager, bookingService,
                    cancellationToken))
            .RequireAuthorization();
        group.MapPost("/bookings/{id:guid}/flytt", async (Guid id, MoveBookingRequest request,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IBookingService bookingService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user?.BusinessId is not Guid businessId)
            {
                return Results.Forbid();
            }

            var result = await bookingService.MoveAsync(id, businessId, request, cancellationToken);
            return result.Outcome switch
            {
                MoveBookingOutcome.Moved => Results.NoContent(),
                MoveBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
                MoveBookingOutcome.SlotTaken => Results.Conflict(
                    new { message = "Den ansatte har allerede en time da. Velg et annet tidspunkt." }),
                _ => Results.NotFound()
            };
        }).RequireAuthorization();
    }

    // Shared logic for panel status changes (confirm / cancel)
    private static async Task<IResult> ChangeBookingStatusAsync(Guid id, BookingStatus newStatus, string? message, ClaimsPrincipal principal,
        UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user?.BusinessId is not Guid businessId)
        {
            return Results.Forbid();
        }

        var outcome = await bookingService.UpdateStatusAsync(id, businessId, newStatus, message, cancellationToken);

        return outcome switch
        {
            UpdateBookingStatusOutcome.Updated => Results.NoContent(),
            UpdateBookingStatusOutcome.InvalidTransition =>
                Results.Conflict(new { message = "Bestillingen kan ikke endres til denne statusen." }),
            _ => Results.NotFound()
        };
    }
}
