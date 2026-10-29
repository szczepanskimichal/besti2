using System.Security.Claims;
using besti2.Application.Notifications;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Endpoints;

// "Varsler på telefonen" (Web Push): the browser asks for the public key, then saves its device here
public static class PushEndpoints
{
    public static void MapPushEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/push");

        // 404 = push is not set up on this server: the web hides the switch
        group.MapGet("/public-key", (IPushService push) =>
            push.PublicKey is null ? Results.NotFound() : Results.Ok(new { publicKey = push.PublicKey }));

        group.MapPost("/subscriptions", async (PushSubscribeRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IPushService push, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }
            var error = await push.SubscribeAsync(user.Id, user.BusinessId, request, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        }).RequireAuthorization();

        // Turned off on this device (POST with the address: DELETE with a body is not reliable everywhere)
        group.MapPost("/subscriptions/remove", async (PushUnsubscribeRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IPushService push, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }
            await push.UnsubscribeAsync(user.Id, request.Endpoint, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private sealed record PushUnsubscribeRequest(string Endpoint);
}
