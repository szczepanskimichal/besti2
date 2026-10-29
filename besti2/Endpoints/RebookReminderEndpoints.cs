using System.Security.Claims;
using besti2.Application.Bookings;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Endpoints;

// "Tid for ny time?": the customer's own reminders to book again
public static class RebookReminderEndpoints
{
    public static void MapRebookReminderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rebook-reminders").RequireAuthorization();

        // Weeks = 0 turns it off
        group.MapPut("", async (SetRebookReminderRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IRebookReminderService reminders, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || user.BusinessId is not null)
            {
                return Results.BadRequest(new { message = "Logg inn med en kundekonto." });
            }
            var error = await reminders.SetAsync(user.Id, request, DateTime.UtcNow, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        group.MapGet("/mine", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IRebookReminderService reminders, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Results.Ok(await reminders.GetMineAsync(userId, cancellationToken));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IRebookReminderService reminders, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is not null && await reminders.RemoveAsync(userId, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        });
    }
}
