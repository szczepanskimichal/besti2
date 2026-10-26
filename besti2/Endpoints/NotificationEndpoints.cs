using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Notifications;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;

namespace besti2.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications");

// The bell (🔔): the newest notifications of the logged-in customer or business, and how many are unread
        app.MapGet("/api/notifications", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            INotificationService notificationService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(await notificationService.GetAsync(user.Id, user.BusinessId, cancellationToken));
        }).RequireAuthorization();
// Only the number on the bell, for the header on every page (cheap: one COUNT)
        app.MapGet("/api/notifications/unread", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            INotificationService notificationService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new
                {
                    unread = await notificationService.UnreadCountAsync(user.Id, user.BusinessId, cancellationToken)
                });
        }).RequireAuthorization();
// The bell, live: an open connection (Server-Sent Events) that sends the number of unread notifications
// at once when it changes, and a small ping every 25 s so proxies keep the connection open
        app.MapGet("/api/notifications/stream", async (HttpContext context, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, INotificationService notificationService) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            var cancellationToken = context.RequestAborted;
            using var subscription = NotificationSignals.Subscribe(NotificationSignals.Key(user.Id, user.BusinessId));

            async Task SendCountAsync()
            {
                var unread = await notificationService.UnreadCountAsync(user.Id, user.BusinessId, cancellationToken);
                await context.Response.WriteAsync($"data: {unread}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }

            try
            {
                await SendCountAsync();
                while (!cancellationToken.IsCancellationRequested)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(25));
                    try
                    {
                        await subscription.Reader.ReadAsync(timeout.Token);
                        await SendCountAsync();
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        await context.Response.WriteAsync(": ping\n\n", cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The browser closed the page: nothing to do
            }
        }).RequireAuthorization();

// "Merk alle som lest"
        app.MapPost("/api/notifications/read", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            INotificationService notificationService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            await notificationService.MarkAllReadAsync(user.Id, user.BusinessId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

// A click on one notification: read, and where it leads
        app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, INotificationService notificationService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var read = await notificationService.MarkReadAsync(user.Id, user.BusinessId, id, cancellationToken);
            return read is null ? Results.NotFound() : Results.Ok(read);
        }).RequireAuthorization();

// The bin next to one notification
        app.MapDelete("/api/notifications/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, INotificationService notificationService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            return await notificationService.DeleteAsync(user.Id, user.BusinessId, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }).RequireAuthorization();
    }
}