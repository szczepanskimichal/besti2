using System.Security.Claims;
using besti2.Application.Accounts;
using besti2.Application.Bookings;
using besti2.Application.Loyalty;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace besti2.Endpoints;

// Min Besti: "Mitt BESTI-år" and the customer's stamp cards
public static class MyYearAndLoyaltyEndpoints
{
    public static void MapMyYearAndLoyaltyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me").RequireAuthorization();

        // year = e.g. 2026; without it the current year in Oslo
        group.MapGet("/year", async (int? year, ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IMyYearService yearService, CancellationToken cancellationToken) =>
        {
            var nowUtc = DateTime.UtcNow;
            var chosen = year ?? TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SlotCalculator.Oslo).Year;
            if (chosen is < 2020 or > 2100)
            {
                return Results.BadRequest(new { message = "Velg et år." });
            }

            return Results.Ok(await yearService.GetAsync(userManager.GetUserId(principal)!, chosen, nowUtc,
                cancellationToken));
        });

        group.MapGet("/loyalty", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
                ILoyaltyService loyaltyService, CancellationToken cancellationToken) =>
            Results.Ok(await loyaltyService.GetMineAsync(userManager.GetUserId(principal)!, DateTime.UtcNow,
                cancellationToken)));
    }
}
