namespace besti2.Application.Businesses;

public interface IBusinessSearchService
{
    // The search page: every filter is optional; date or time of day adds free times to the cards
    Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(
        string? category = null, string? city = null, string? q = null, DateOnly? date = null,
        TimeOnly? from = null, TimeOnly? to = null, double? lat = null, double? lng = null, double? maxKm = null,
        string? sort = null, int? minRating = null, CancellationToken cancellationToken = default);

    // Cities with at least one visible business, for the Hvor field
    Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default);
}