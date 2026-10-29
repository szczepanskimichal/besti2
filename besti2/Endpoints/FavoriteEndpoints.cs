using System.Security.Claims;
using besti2.Application.Favorites;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Endpoints;

// "Favoritter": the customer's saved businesses (the heart on the business page, the tab in Min Besti)
public static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me/favorites").RequireAuthorization();

        group.MapGet("", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
                IFavoriteService favoriteService, CancellationToken cancellationToken) =>
            Results.Ok(await favoriteService.GetMineAsync(userManager.GetUserId(principal)!, DateTime.UtcNow,
                cancellationToken)));

        group.MapGet("/ids", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
                IFavoriteService favoriteService, CancellationToken cancellationToken) =>
            Results.Ok(await favoriteService.GetIdsAsync(userManager.GetUserId(principal)!, cancellationToken)));

        group.MapPut("/{businessId:guid}", async (Guid businessId, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IFavoriteService favoriteService, CancellationToken cancellationToken) =>
        {
            var error = await favoriteService.AddAsync(userManager.GetUserId(principal)!, businessId, DateTime.UtcNow,
                cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        group.MapDelete("/{businessId:guid}", async (Guid businessId, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IFavoriteService favoriteService, CancellationToken cancellationToken) =>
        {
            await favoriteService.RemoveAsync(userManager.GetUserId(principal)!, businessId, cancellationToken);
            return Results.NoContent();
        });
    }
}
