namespace besti2.Application.Accounts;

// "Mitt BESTI-år": the customer's own year in numbers (only visits that took place)
public record MyYearDto(
    int Year,
    int Visits,
    int Businesses,
    int MinutesOfMeTime,
    decimal Spent,
    int VisitsWithFriends,
    int ReviewsWritten,
    string? FavoriteBusiness,
    string? FavoriteBusinessSlug,
    int FavoriteBusinessVisits,
    string? FavoriteService,
    int? BusiestMonth, // 1-12
    DateTime? FirstVisitUtc,
    IReadOnlyList<int> Months, // 12 numbers, January first
    IReadOnlyList<int> Years); // the years the customer has visits in, newest first (for the year picker)

public interface IMyYearService
{
    Task<MyYearDto> GetAsync(string userId, int year, DateTime nowUtc, CancellationToken cancellationToken = default);
}
