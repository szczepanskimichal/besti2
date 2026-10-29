using System.Security.Claims;
using besti2.Application.Bookings;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Endpoints;

// "Si fra hvis det blir ledig" (venteliste), for customers with an account
public static class WaitlistEndpoints
{
    public static void MapWaitlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/waitlist").RequireAuthorization();

        group.MapPost("", async (JoinWaitlistRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IWaitlistService waitlist, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || user.BusinessId is not null)
            {
                return Results.BadRequest(new { message = "Logg inn med en kundekonto for å stå på venteliste." });
            }
            var error = await waitlist.JoinAsync(user.Id, request, DateTime.UtcNow, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireRateLimiting("booking");

        // Min Besti: the days I am waiting for
        group.MapGet("/mine", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IWaitlistService waitlist, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Results.Ok(await waitlist.GetMineAsync(userId, DateTime.UtcNow, cancellationToken));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IWaitlistService waitlist, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is not null && await waitlist.LeaveAsync(userId, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        });
    }
}
