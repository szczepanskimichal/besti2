using besti2.Application.Bookings;
using besti2.Application.Businesses;
using besti2.Application.Images;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// The customer's search: filters, free times on the cards, "Nær meg", "Best vurdert" and the city list
public class BusinessSearchService(AppDbContext dbContext, IImageStorage imageStorage) : IBusinessSearchService
{
    private static readonly TimeZoneInfo Oslo = SlotCalculator.Oslo;

    public async Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(
        string? category = null, string? city = null, string? q = null, DateOnly? date = null,
        TimeOnly? from = null, TimeOnly? to = null, double? lat = null, double? lng = null, double? maxKm = null,
        string? sort = null, int? minRating = null, CancellationToken cancellationToken = default)
    {
        // Hidden businesses (owner has not confirmed the email yet) are never shown, and neither are
        // businesses without any service yet (nothing to book, the page would look dead)
        var query = dbContext.Businesses.AsNoTracking()
            .Where(b => !b.IsHidden && b.Services.Any(s => !s.IsDeleted));
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<BusinessCategory>(category, ignoreCase: true, out var parsedCategory)
                || !Enum.IsDefined(parsedCategory))
            {
                return [];
            }

            query = query.Where(b => b.Category == parsedCategory);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim().ToLower();
            query = query.Where(b => b.City.ToLower().Contains(trimmedCity));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var text = q.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(text)
                                     || b.Services.Any(s => !s.IsDeleted && s.Name.ToLower().Contains(text)));
        }

        var items = await query
            .OrderBy(b => b.Name)
            .Select(b => new BusinessListItemDto(
                b.Id,
                b.Name,
                b.Category.ToString(),
                b.Description,
                b.City,
                b.Images.Where(i => i.Kind == ImageKind.Logo).Select(i => i.FileName).FirstOrDefault(),
                null,
                null,
                // Stars for the card, counted in the same query (no extra request per business)
                dbContext.Reviews.Where(r => r.BusinessId == b.Id).Average(r => (double?)r.Stars),
                dbContext.Reviews.Count(r => r.BusinessId == b.Id)
            ))
            .ToListAsync(cancellationToken);

        var withLogos = items.Select(b => b with
        {
            LogoUrl = ToLogoUrl(b.LogoUrl),
            AverageRating = b.AverageRating is double average ? Math.Round(average, 1) : null
        }).ToList();
        // "4★ og over": the same rounded average the customer sees on the card;
        // new businesses (under 3 reviews, no average shown yet) are never left out
        if (minRating is int min)
        {
            withLogos = withLogos.Where(b => b.ReviewCount < MinReviewsForAverage || b.AverageRating >= min).ToList();
        }

        // Only a time of day: the soonest day for each business
        var list = date is not null
            ? await AddFreeSlotsAsync(withLogos, date.Value, q, from, to, cancellationToken)
            : from is not null || to is not null
                ? await FindSoonestSlotsAsync(withLogos, q, from, to, cancellationToken)
                : withLogos;
        var sorted = lat is double userLat && lng is double userLng
            ? await SortByDistanceAsync(list, userLat, userLng, maxKm, cancellationToken)
            : list;
        // "Best vurdert": highest score first (OrderBy keeps the old order, e.g. distance, for equal scores)
        var ordered = sort == "rating"
            ? sorted.OrderByDescending(b => RatingScore(b.AverageRating, b.ReviewCount)).ToList()
            : sorted;
        // "Ny på Besti": no average before 3 reviews, so one bad review cannot sink a new business
        return ordered
            .Select(b => b.ReviewCount < MinReviewsForAverage ? b with { AverageRating = null } : b)
            .ToList();
    }

    // Bayesian average: every business starts with 10 imagined reviews of 4 stars, so one bad review
    // barely moves a small business, and one 5★ review does not beat forty 4.8★ reviews.
    // The more real reviews, the closer the score gets to the real average.
    private const double PriorStars = 4.0;
    private const int PriorCount = 10;
    public const int MinReviewsForAverage = 3;

    public static double RatingScore(double? average, int count) =>
        average is double stars ? (PriorStars * PriorCount + stars * count) / (PriorCount + count) : 0;

    // Free times for the whole list with 5 queries, however many businesses there are (no query per business)
    private async Task<IReadOnlyList<BusinessListItemDto>> AddFreeSlotsAsync(
        List<BusinessListItemDto> businesses, DateOnly date, string? q, TimeOnly? from, TimeOnly? to,
        CancellationToken cancellationToken = default)
    {
        // Outside any sensible booking range (also avoids a DateTime overflow for 9999-12-31)
        if (date.Year is < 2000 or > 2100)
        {
            return [];
        }

        var businessIds = businesses.Select(b => b.Id).ToList();

        var services = await dbContext.Services.AsNoTracking()
            .Where(s => businessIds.Contains(s.BusinessId) && !s.IsDeleted)
            .Select(s => new { s.Id, s.BusinessId, s.Name, s.DurationMinutes })
            .ToListAsync(cancellationToken);

        var employees = await dbContext.Employees.AsNoTracking()
            .Where(e => businessIds.Contains(e.BusinessId) && !e.IsDeleted)
            .Select(e => new { e.Id, e.BusinessId })
            .ToListAsync(cancellationToken);

        var hours = await dbContext.OpeningHours.AsNoTracking()
            .Where(h => businessIds.Contains(h.BusinessId) && h.DayOfWeek == date.DayOfWeek)
            .ToListAsync(cancellationToken);

        // Same wide UTC window as AvailabilityService.GetFreeSlotsAsync (the whole Oslo day is inside it)
        var fromUtc = date.ToDateTime(TimeOnly.MinValue).AddDays(-1);

        var toUtc = date.ToDateTime(TimeOnly.MinValue).AddDays(2);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var bookings = await dbContext.Bookings.AsNoTracking()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.Status != BookingStatus.Avlyst
                        && b.StartUtc < toUtc
                        && b.EndUtc > fromUtc)
            .Select(b => new { b.EmployeeId, b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);
        // Blocked time: EmployeeId null = the whole business is closed
        var timeOffs = await dbContext.TimeOffs.AsNoTracking()
            .Where(t => businessIds.Contains(t.BusinessId)
                        && t.StartUtc < toUtc
                        && t.EndUtc > fromUtc)
            .Select(t => new { t.BusinessId, t.EmployeeId, t.StartUtc, t.EndUtc })
            .ToListAsync(cancellationToken);
        // The same earliest time as on the booking page (see BookingRules)
        var earliestUtc = BookingRules.EarliestStartUtc(DateTime.UtcNow);

        var text = q?.Trim().ToLower();

        var result = new List<BusinessListItemDto>();

        foreach (var business in businesses)
        {
            var dayHours = hours.FirstOrDefault(h => h.BusinessId == business.Id);

            var businessServices = services.Where(s => s.BusinessId == business.Id).ToList();

            // The searched service ("klipp" -> Herreklipp), otherwise the shortest one
            var service = businessServices
                              .Where(s => !string.IsNullOrEmpty(text) && s.Name.ToLower().Contains(text))
                              .OrderBy(s => s.DurationMinutes)
                              .FirstOrDefault()
                          ?? businessServices.OrderBy(s => s.DurationMinutes).FirstOrDefault();

            if (dayHours is null || service is null)
            {
                continue; // closed that day, or no services
            }

            var slots = employees
                .Where(e => e.BusinessId == business.Id)
                .SelectMany(e => SlotCalculator.GetFreeSlots(
                        date, dayHours.OpensAt, dayHours.ClosesAt, service.DurationMinutes,
                        bookings.Where(b => b.EmployeeId == e.Id).Select(b => (b.StartUtc, b.EndUtc))
                            .Concat(timeOffs.Where(t => t.BusinessId == business.Id
                                                        && (t.EmployeeId == null || t.EmployeeId == e.Id))
                                .Select(t => (t.StartUtc, t.EndUtc)))
                            .ToList(), earliestUtc)
                    .Where(start => IsInTimeWindow(start, from, to))
                    .Select(start => new FreeSlotDto(start, service.Id, service.Name, e.Id)))
                .OrderBy(s => s.StartUtc)
                .DistinctBy(s => s.StartUtc) // two free employees at 10:00 = one button
                .Take(3)
                .ToList();
            if (slots.Count > 0)
            {
                result.Add(business with { FreeSlots = slots });
            }
        }

        // Soonest free time first
        return result.OrderBy(b => b.FreeSlots![0].StartUtc).ToList();
    }

    // "Før 12" = to 12:00, "Etter 17" = from 17:00; compared in Oslo time
    private static bool IsInTimeWindow(DateTime startUtc, TimeOnly? from, TimeOnly? to)
    {
        var local = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, Oslo));
        return (from is null || local >= from) && (to is null || local < to);
    }

    // "Tid på dagen" without a date: for each business the soonest day (max 14 days ahead) with a free time in that window
    private async Task<IReadOnlyList<BusinessListItemDto>> FindSoonestSlotsAsync(
        List<BusinessListItemDto> businesses, string? q, TimeOnly? from, TimeOnly? to,
        CancellationToken cancellationToken = default)
    {
        const int daysAhead = 14;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Oslo));
        var found = new List<BusinessListItemDto>();
        var left = businesses;
        for (var day = 0; day < daysAhead && left.Count > 0; day++)
        {
            var withSlots = await AddFreeSlotsAsync(left, today.AddDays(day), q, from, to, cancellationToken);
            found.AddRange(withSlots);
            left = left.Where(b => withSlots.All(w => w.Id != b.Id)).ToList();
        }

        // Earlier days were added first and each day is sorted by time, so the soonest is already first
        return found;
    }

    // "Nær meg": distance from the customer to each business, nearest first; businesses without a map point go last
    private async Task<IReadOnlyList<BusinessListItemDto>> SortByDistanceAsync(
        IReadOnlyList<BusinessListItemDto> businesses, double lat, double lng, double? maxKm,
        CancellationToken cancellationToken)
    {
        var ids = businesses.Select(b => b.Id).ToList();
        var points = await dbContext.Businesses.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Latitude, b.Longitude })
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        return businesses
            .Select(b => points[b.Id] is { Latitude: double bLat, Longitude: double bLng }
                ? b with { DistanceKm = Math.Round(HaversineKm(lat, lng, bLat, bLng), 1) }
                : b)
            // With a radius: only businesses that close (no map point = unknown distance = left out)
            .Where(b => maxKm is null || b.DistanceKm <= maxKm)
            .OrderBy(b => b.DistanceKm ?? double.MaxValue)
            .ToList();
    }

    // Distance in a straight line between two points on Earth (haversine formula)
    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                                                 * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public async Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses
            .AsNoTracking()
            .Where(b => !b.IsHidden && b.Services.Any(s => !s.IsDeleted))
            .Select(b => b.City)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync(cancellationToken);
    }

    // The database keeps only the file name; the storage knows the public address
    private string? ToLogoUrl(string? fileName) => fileName is null ? null : imageStorage.GetUrl(fileName);
}