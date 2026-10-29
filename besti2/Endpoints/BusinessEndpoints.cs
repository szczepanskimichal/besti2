using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Infrastructure.Identity;
using besti2.Application.Businesses;
using besti2.Application.Reviews;

namespace besti2.Endpoints;

public static class BusinessEndpoints
{
    public static void MapBusinessEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // Get all businesses (optional filters: category, city, text q, date + from/to = free times that day,
        // sort=rating = "Best vurdert", minRating=4 = "4★ og over")
        group.MapGet("/businesses",
            async (string? category, string? city, string? q, DateOnly? date, TimeOnly? from, TimeOnly? to, double? lat,
                double? lng, double? maxKm, string? sort, int? minRating, int? page, int? persons,
                IBusinessSearchService search, CancellationToken cancellationToken) =>
            {
                // A position outside the map (or "infinity") would crash the database query
                if (lat is not (null or >= -90 and <= 90) || lng is not (null or >= -180 and <= 180))
                {
                    return Results.BadRequest(new { message = "Ugyldig posisjon." });
                }

                // A radius of 0, below 0 or bigger than Norway = no radius
                if (maxKm is not (> 0 and <= 2000))
                {
                    maxKm = null;
                }

                var all = await search.GetAllAsync(category, city, q, date, from, to, lat, lng, maxKm, sort, minRating,
                    persons ?? 1, cancellationToken);
                // 24 businesses per page (?page=2 ...)
                return Results.Ok(BusinessPageDto.From(all, page));
            }).RequireRateLimiting("search").CacheOutput("search");
        // Get distinct cities for the filter
        group.MapGet("/cities",
            async (IBusinessSearchService search, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await search.GetCitiesAsync(cancellationToken));
            }).CacheOutput("search");
        // Get business by id
        group.MapGet("/businesses/{id:guid}",
            async (Guid id, IBusinessService businessService, CancellationToken cancellationToken) =>
            {
                var business = await businessService.GetByIdAsync(id, cancellationToken);
                return business is null ? Results.NotFound() : Results.Ok(business);
            });
        // Get business by its short page address (bestiapp.no/klipp-og-stil-frisor-gjovik)
        group.MapGet("/businesses/by-slug/{slug}",
            async (string slug, IBusinessService businessService, CancellationToken cancellationToken) =>
            {
                var business = await businessService.GetBySlugAsync(slug, cancellationToken);
                return business is null ? Results.NotFound() : Results.Ok(business);
            });
        // Register a new business with its owner account
        group.MapPost("/businesses/register", async (RegisterBusinessRequest request,
            IBusinessRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            var result = await registrationService.RegisterAsync(request, cancellationToken);
            return result.Success
                ? Results.Created()
                : Results.BadRequest(new { message = result.Error });
        }).RequireRateLimiting("signup");

        // Public: reviews of a business, newest first, with the average. A logged-in reader also learns
        // which ones they marked "Nyttig"
        group.MapGet("/businesses/{id:guid}/reviews", async (Guid id, ClaimsPrincipal principal,
                UserManager<AppUser> userManager, IReviewService reviewService, CancellationToken cancellationToken) =>
            Results.Ok(await reviewService.GetForBusinessAsync(id, viewerUserId: userManager.GetUserId(principal),
                cancellationToken: cancellationToken)));
        // "Nyttig" on a review (on and off); only for logged-in customers
        group.MapPost("/reviews/{id:guid}/helpful", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IReviewService reviewService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || user.BusinessId is not null)
            {
                return Results.Forbid();
            }

            var result = await reviewService.ToggleHelpfulAsync(user.Id, id, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();
        // Business sign-up: "Hent fra Brønnøysund" – name and address of an active business in the register
        group.MapGet("/brreg/{orgNumber}", async (string orgNumber, IBusinessRegistry registry,
            CancellationToken cancellationToken) =>
        {
            if (!OrgNumber.IsValid(orgNumber))
            {
                return Results.BadRequest(new
                    { message = "Ugyldig organisasjonsnummer. Sjekk at det er skrevet riktig." });
            }

            var lookup = await registry.LookupAsync(orgNumber, cancellationToken);
            return lookup.Status switch
            {
                RegistryStatus.Active => Results.Ok(new
                {
                    lookup.Name, lookup.Address, lookup.PostalCode, lookup.City
                }),
                RegistryStatus.NotFound => Results.NotFound(new
                    { message = "Fant ikke virksomheten i Enhetsregisteret." }),
                RegistryStatus.Closed => Results.Conflict(
                    new { message = "Virksomheten er slettet, konkurs eller under avvikling i Enhetsregisteret." }),
                _ => Results.Problem("Brønnøysundregistrene svarer ikke akkurat nå. Fyll ut feltene selv.",
                    statusCode: 503)
            };
        }).RequireRateLimiting("signup");
    }
}