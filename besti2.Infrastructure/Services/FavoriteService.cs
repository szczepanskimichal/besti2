using besti2.Application.Favorites;
using besti2.Application.Images;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class FavoriteService(AppDbContext dbContext, IImageStorage imageStorage) : IFavoriteService
{
    public async Task<string?> AddAsync(string userId, Guid businessId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Favorites.AnyAsync(f => f.UserId == userId && f.BusinessId == businessId, cancellationToken))
        {
            return null;
        }

        // Only businesses customers can see
        if (!await dbContext.Businesses.AnyAsync(b => b.Id == businessId && !b.IsHidden, cancellationToken))
        {
            return "Fant ikke bedriften.";
        }

        if (await dbContext.Favorites.CountAsync(f => f.UserId == userId, cancellationToken) >= IFavoriteService.MaxFavorites)
        {
            return $"Du kan ha maks {IFavoriteService.MaxFavorites} favoritter. Fjern en først.";
        }

        dbContext.Favorites.Add(new Favorite
            { Id = Guid.NewGuid(), UserId = userId, BusinessId = businessId, CreatedUtc = nowUtc });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two clicks at the same moment: the first one saved it
        }

        return null;
    }

    public async Task RemoveAsync(string userId, Guid businessId, CancellationToken cancellationToken = default)
    {
        var favorite = await dbContext.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BusinessId == businessId, cancellationToken);
        if (favorite is not null)
        {
            dbContext.Favorites.Remove(favorite);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetIdsAsync(string userId, CancellationToken cancellationToken = default) =>
        await dbContext.Favorites.Where(f => f.UserId == userId).Select(f => f.BusinessId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FavoriteDto>> GetMineAsync(string userId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var favorites = await dbContext.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId && !f.Business.IsHidden)
            .OrderByDescending(f => f.CreatedUtc)
            .Select(f => new
            {
                f.BusinessId,
                f.Business.Name,
                f.Business.Slug,
                f.Business.City,
                f.Business.Category,
                Logo = f.Business.Images.Where(i => i.Kind == ImageKind.Logo).Select(i => i.FileName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        var ids = favorites.Select(f => f.BusinessId).ToList();

        var ratings = await dbContext.Reviews.Where(r => ids.Contains(r.BusinessId))
            .GroupBy(r => r.BusinessId)
            .Select(g => new { g.Key, Average = g.Average(r => r.Stars), Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, cancellationToken);
        var visits = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.CustomerUserId == userId && ids.Contains(b.Service.BusinessId)
                                                   && b.Status != BookingStatus.Avlyst)
            .Select(b => new
            {
                b.Service.BusinessId, b.StartUtc, b.EndUtc, b.Status, b.ServiceId, ServiceName = b.Service.Name,
                ServiceDeleted = b.Service.IsDeleted, b.EmployeeId, EmployeeName = b.Employee.Name,
                EmployeeDeleted = b.Employee.IsDeleted
            })
            .ToListAsync(cancellationToken);

        return favorites.Select(f =>
        {
            var last = visits.Where(v => v.BusinessId == f.BusinessId && v.Status == BookingStatus.Bekreftet
                                                                      && v.EndUtc <= nowUtc)
                .MaxBy(v => v.StartUtc);
            var next = visits.Where(v => v.BusinessId == f.BusinessId && v.StartUtc > nowUtc
                                                                      && v.Status != BookingStatus.MotteIkke)
                .MinBy(v => v.StartUtc);
            ratings.TryGetValue(f.BusinessId, out var rating);
            return new FavoriteDto(
                f.BusinessId, f.Name, f.Slug, f.City, f.Category.ToString(),
                f.Logo is null ? null : imageStorage.GetUrl(f.Logo),
                rating is null ? null : Math.Round(rating.Average, 1),
                rating?.Count ?? 0,
                last is null
                    ? null
                    : new FavoriteLastVisitDto(last.StartUtc, last.ServiceId, last.ServiceName, last.EmployeeId,
                        last.EmployeeName, ServiceAvailable: !last.ServiceDeleted,
                        EmployeeAvailable: !last.EmployeeDeleted),
                next?.StartUtc);
        }).ToList();
    }
}
