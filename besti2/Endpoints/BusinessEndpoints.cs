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
                double? lng, double? maxKm, string? sort, int? minRating, int? page,
                IBusinessSearchService search, CancellationToken cancellationToken) =>
            {
                var all = await search.GetAllAsync(category, city, q, date, from, to, lat, lng, maxKm, sort, minRating,
                    cancellationToken);
                // 24 businesses per page (?page=2 ...)
                return Results.Ok(BusinessPageDto.From(all, page));
            });
        // Get distinct cities for the filter
        group.MapGet("/cities",
            async (IBusinessSearchService search, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await search.GetCitiesAsync(cancellationToken));
            });
        // Get business by id
        group.MapGet("/businesses/{id:guid}",
            async (Guid id, IBusinessService businessService, CancellationToken cancellationToken) =>
            {
                var business = await businessService.GetByIdAsync(id, cancellationToken);
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

        // Public: reviews of a business, newest first, with the average
        group.MapGet("/businesses/{id:guid}/reviews", async (Guid id, IReviewService reviewService,
                CancellationToken cancellationToken) =>
            Results.Ok(await reviewService.GetForBusinessAsync(id, cancellationToken)));
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