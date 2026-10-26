using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Bookings;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // Get availability for a service and employee on a specific date
        group.MapGet("/availability", async (Guid serviceId, Guid employeeId, DateOnly date, IBookingService bookingService,
            CancellationToken cancellationToken) =>
        {
            var slots = await bookingService.GetFreeSlotsAsync(serviceId, employeeId, date, cancellationToken);
            return slots is null ? Results.NotFound() : Results.Ok(slots);
        });

        // "Velg dag": free times for 7 days from a date; without employeeId = anyone free
        group.MapGet("/availability/days", async (Guid serviceId, Guid? employeeId, DateOnly from,
            IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var days = await bookingService.GetFreeDaysAsync(serviceId, employeeId, from, 7, cancellationToken);
            return days is null ? Results.NotFound() : Results.Ok(days);
        });
        // Create a new booking (customers only; name and email come from the account)
        group.MapPost("/bookings",
            async (CreateBookingRequest request, ClaimsPrincipal principal, UserManager<AppUser> userManager,
                IBookingService bookingService, CancellationToken cancellationToken) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null || user.BusinessId is not null)
                {
                    return Results.BadRequest(new { message = "Logg inn med en kundekonto for å bestille." });
                }

                // Never trust name/email from the form: use the account's
                var fromAccount = request with { CustomerName = user.FullName ?? "", CustomerEmail = user.Email ?? "" };
                var result = await bookingService.CreateAsync(fromAccount, user.Id, cancellationToken);
                return result.Outcome switch
                {
                    CreateBookingOutcome.Created => Results.Created($"/api/bookings/{result.Booking!.Id}", result.Booking),
                    CreateBookingOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
                    CreateBookingOutcome.SlotTaken => Results.Conflict(
                        new { message = "Tiden er dessverre ikke lenger ledig." }),
                    _ => Results.NotFound()
                };
            }).RequireAuthorization().RequireRateLimiting("booking");

        // Customer: own bookings for "Mine bestillinger"
        group.MapGet("/me/bookings", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Results.Ok(await bookingService.GetForCustomerAsync(userId, cancellationToken));
        }).RequireAuthorization();

        // Customer: cancel own booking before it starts
        group.MapPost("/me/bookings/{id:guid}/avbestill", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            return await bookingService.CancelByCustomerAsync(id, userId, cancellationToken) switch
            {
                UpdateBookingStatusOutcome.Updated => Results.NoContent(),
                UpdateBookingStatusOutcome.InvalidTransition => Results.BadRequest(
                    new { message = "Bestillingen kan ikke avbestilles lenger." }),
                _ => Results.NotFound()
            };
        }).RequireAuthorization();
        // "Ny tid": the moved visit behind the one-time link in the e-mail (no login, so it works for every customer)
        group.MapGet("/ny-tid", async (string? token, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var newTime = await bookingService.GetNewTimeAsync(token ?? "", cancellationToken);
            return newTime is null ? Results.NotFound() : Results.Ok(newTime);
        }).RequireRateLimiting("booking");

        // The customer's answer from the link: Godta (the business confirms next) or Passer ikke (cancelled)
        group.MapPost("/ny-tid", async (NewTimeAnswerRequest request, IBookingService bookingService,
                    CancellationToken cancellationToken) =>
                NewTimeResult(await bookingService.AnswerNewTimeAsync(request.Token, request.Accept, cancellationToken)))
            .RequireRateLimiting("booking");

        // The same answer from Min Besti (logged in)
        group.MapPost("/me/bookings/{id:guid}/ny-tid", async (Guid id, NewTimeAnswer request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            return NewTimeResult(
                await bookingService.AnswerNewTimeByCustomerAsync(id, userId, request.Accept, cancellationToken));
        }).RequireAuthorization();
    }

    private static IResult NewTimeResult(NewTimeOutcome outcome) => outcome switch
    {
        NewTimeOutcome.Accepted or NewTimeOutcome.Declined => Results.Ok(new { outcome = outcome.ToString() }),
        NewTimeOutcome.TooLate => Results.BadRequest(new { message = "Timen har allerede startet." }),
        _ => Results.NotFound(new { message = "Lenken er ikke gyldig lenger. Kanskje du allerede har svart?" })
    };
}
