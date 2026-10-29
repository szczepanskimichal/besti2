using besti2.Application.Accounts;
using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// "Mitt BESTI-år": counted from the customer's own visits that took place (the year in Oslo)
public class MyYearService(AppDbContext dbContext) : IMyYearService
{
    public async Task<MyYearDto> GetAsync(string userId, int year, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var visits = (await dbContext.Bookings.AsNoTracking()
                .Where(b => b.CustomerUserId == userId && b.Status == BookingStatus.Bekreftet && b.EndUtc <= nowUtc)
                .Select(b => new
                {
                    b.StartUtc, b.EndUtc, b.PriceNok, b.InviteId, b.Service.BusinessId, BusinessName = b.Service.Business.Name,
                    b.Service.Business.Slug, ServiceName = b.Service.Name
                })
                .ToListAsync(cancellationToken))
            .Select(v => new { Visit = v, Oslo = TimeZoneInfo.ConvertTimeFromUtc(v.StartUtc, SlotCalculator.Oslo) })
            .ToList();
        var years = visits.Select(v => v.Oslo.Year).Distinct().OrderDescending().ToList();
        var inYear = visits.Where(v => v.Oslo.Year == year).ToList();

        var months = new int[12];
        foreach (var v in inYear)
        {
            months[v.Oslo.Month - 1]++;
        }

        var favorite = inYear.GroupBy(v => new { v.Visit.BusinessId, v.Visit.BusinessName, v.Visit.Slug })
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        var favoriteService = inYear.GroupBy(v => v.Visit.ServiceName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
        var startUtc = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-2);
        var endUtc = startUtc.AddYears(1).AddHours(4);
        var reviews = (await dbContext.Reviews.AsNoTracking()
                .Where(r => r.CustomerUserId == userId && r.CreatedUtc >= startUtc && r.CreatedUtc < endUtc)
                .Select(r => r.CreatedUtc)
                .ToListAsync(cancellationToken))
            .Count(c => TimeZoneInfo.ConvertTimeFromUtc(c, SlotCalculator.Oslo).Year == year);

        return new MyYearDto(
            year,
            Visits: inYear.Count,
            Businesses: inYear.Select(v => v.Visit.BusinessId).Distinct().Count(),
            MinutesOfMeTime: (int)inYear.Sum(v => (v.Visit.EndUtc - v.Visit.StartUtc).TotalMinutes),
            Spent: inYear.Sum(v => v.Visit.PriceNok ?? 0),
            VisitsWithFriends: inYear.Count(v => v.Visit.InviteId != null),
            ReviewsWritten: reviews,
            FavoriteBusiness: favorite?.Key.BusinessName,
            FavoriteBusinessSlug: favorite?.Key.Slug,
            FavoriteBusinessVisits: favorite?.Count() ?? 0,
            favoriteService,
            BusiestMonth: inYear.Count == 0 ? null : Array.IndexOf(months, months.Max()) + 1,
            FirstVisitUtc: inYear.Count == 0 ? null : inYear.Min(v => v.Visit.StartUtc),
            months,
            years);
    }
}
