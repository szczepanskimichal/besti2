namespace besti2.Application.Favorites;

public interface IFavoriteService
{
    // At most this many hearts per customer
    const int MaxFavorites = 50;

    // Saves the heart (again is fine); null = saved, otherwise the error for the customer
    Task<string?> AddAsync(string userId, Guid businessId, DateTime nowUtc, CancellationToken cancellationToken = default);

    // Removes the heart (also when it was not there)
    Task RemoveAsync(string userId, Guid businessId, CancellationToken cancellationToken = default);

    // The business ids the customer has saved (the business page shows a filled heart for them)
    Task<IReadOnlyList<Guid>> GetIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteDto>> GetMineAsync(string userId, DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
