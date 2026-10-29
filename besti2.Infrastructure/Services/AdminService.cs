using besti2.Application.Admin;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class AdminService(AppDbContext dbContext) : IAdminService
{
    public const int NoteMaxLength = 2000;

    public async Task<AdminOverviewDto> GetOverviewAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var rows = await RowsAsync(dbContext.Businesses, nowUtc, cancellationToken);
        var monthAgo = nowUtc.AddDays(-30);
        return new AdminOverviewDto(
            VisibleBusinesses: rows.Count(r => r.Status == "Synlig"),
            WaitingForApproval: rows.Count(r => r.Status == "Venter"),
            UnconfirmedSignups: rows.Count(r => r.Status == "Ubekreftet"),
            CustomerAccounts: await dbContext.Users.CountAsync(u => u.BusinessId == null, cancellationToken),
            BookingsLast7Days: await dbContext.Bookings.CountAsync(b => b.CreatedUtc >= nowUtc.AddDays(-7),
                cancellationToken),
            VisitsLast30Days: await dbContext.Bookings.CountAsync(b => b.Status == BookingStatus.Bekreftet
                                                                       && b.StartUtc >= monthAgo
                                                                       && b.EndUtc <= nowUtc, cancellationToken),
            ReviewsLast30Days: await dbContext.Reviews.CountAsync(r => r.CreatedUtc >= monthAgo, cancellationToken),
            ProInterested: rows.Count(r => r.ProInterest),
            Waiting: rows.Where(r => r.Status == "Venter").OrderBy(r => r.CreatedUtc).ToList(),
            // Visible, but customers cannot really book yet, or nobody has booked in a month: worth a call
            NeedHelp: rows.Where(r => r.Status == "Synlig"
                                      && (r.SetupDone < r.SetupTotal - 1 || r.BookingsLast30Days == 0))
                .OrderBy(r => r.SetupDone)
                .ThenBy(r => r.CreatedUtc)
                .Take(10)
                .ToList());
    }

    public async Task<IReadOnlyList<AdminBusinessRow>> GetBusinessesAsync(string? status, string? q, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Businesses.AsQueryable();
        var search = q?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var ownerIds = dbContext.Users.Where(u => u.BusinessId != null && u.Email!.Contains(search))
                .Select(u => u.BusinessId);
            query = query.Where(b => b.Name.Contains(search) || b.City.Contains(search) || b.OrgNumber == search
                                     || ownerIds.Contains(b.Id));
        }

        var rows = await RowsAsync(query, nowUtc, cancellationToken);
        return rows.Where(r => string.IsNullOrEmpty(status) || r.Status == status)
            .OrderBy(r => r.Status == "Venter" ? 0 : 1)
            .ThenByDescending(r => r.CreatedUtc)
            .ToList();
    }

    public async Task<AdminBusinessDetailsDto?> GetBusinessAsync(Guid id, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var row = (await RowsAsync(dbContext.Businesses.Where(b => b.Id == id), nowUtc, cancellationToken)).Single();
        var setup = await SetupAsync(dbContext.Businesses.Where(b => b.Id == id), cancellationToken);
        return new AdminBusinessDetailsDto(
            row,
            business.OrgNumber,
            business.Address,
            business.PostalCode,
            business.Email,
            business.Website,
            business.ReferralSource,
            business.ApprovedUtc,
            business.AdminNote,
            WeeklyReportOn: !business.WeeklyReportOff,
            ReviewCount: await dbContext.Reviews.CountAsync(r => r.BusinessId == id, cancellationToken),
            LastBookingCreatedUtc: await dbContext.Bookings.Where(b => b.Service.BusinessId == id)
                .MaxAsync(b => (DateTime?)b.CreatedUtc, cancellationToken),
            Setup: Steps(setup[id]));
    }

    public async Task<string?> SetNoteAsync(Guid id, string? note, CancellationToken cancellationToken = default)
    {
        var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (text?.Length > NoteMaxLength)
        {
            return $"Notatet kan være maks {NoteMaxLength} tegn.";
        }

        var business = await dbContext.Businesses.FindAsync([id], cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        business.AdminNote = text;
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<AdminReviewDto>> GetLatestReviewsAsync(int count,
        CancellationToken cancellationToken = default) =>
        await (from review in dbContext.Reviews.AsNoTracking()
                join business in dbContext.Businesses on review.BusinessId equals business.Id
                orderby review.CreatedUtc descending
                select new AdminReviewDto(review.Id, business.Name, business.Slug, business.Id, review.CustomerName,
                    review.Stars, review.Comment, review.BusinessReply, review.CreatedUtc))
            .Take(Math.Clamp(count, 1, 200))
            .ToListAsync(cancellationToken);

    private async Task<List<AdminBusinessRow>> RowsAsync(IQueryable<Business> businesses, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var list = await businesses.AsNoTracking()
            .Select(b => new
            {
                b.Id, b.Name, b.Slug, b.City, b.Category, b.IsHidden, b.ApprovedUtc, b.ApprovalTokenHash, b.CreatedUtc,
                b.PhoneNumber, b.VerifiedUtc, b.ProInterestUtc, b.AdminNote
            })
            .ToListAsync(cancellationToken);
        var ids = list.Select(b => b.Id).ToList();
        var monthAgo = nowUtc.AddDays(-30);
        var owners = await dbContext.Users.Where(u => u.BusinessId != null && ids.Contains(u.BusinessId.Value))
            .Select(u => new { u.BusinessId, u.Email })
            .ToListAsync(cancellationToken);
        var bookings = await dbContext.Bookings.Where(b => ids.Contains(b.Service.BusinessId) && b.CreatedUtc >= monthAgo)
            .GroupBy(b => b.Service.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var upcoming = await dbContext.Bookings.Where(b => ids.Contains(b.Service.BusinessId) && b.StartUtc >= nowUtc
                                                           && b.Status != BookingStatus.Avlyst)
            .GroupBy(b => b.Service.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var stars = await dbContext.Reviews.Where(r => ids.Contains(r.BusinessId))
            .GroupBy(r => r.BusinessId)
            .Select(g => new { g.Key, Average = g.Average(r => r.Stars) })
            .ToDictionaryAsync(g => g.Key, g => g.Average, cancellationToken);
        var setup = await SetupAsync(businesses, cancellationToken);

        return list.Select(b =>
        {
            var steps = Steps(setup[b.Id]);
            return new AdminBusinessRow(
                b.Id, b.Name, b.Slug, b.City, b.Category.ToString(),
                Status: !b.IsHidden ? "Synlig"
                    : b.ApprovalTokenHash != null ? "Venter"
                    : b.ApprovedUtc == null ? "Ubekreftet"
                    : "Skjult",
                b.CreatedUtc,
                owners.FirstOrDefault(o => o.BusinessId == b.Id)?.Email,
                b.PhoneNumber,
                Verified: b.VerifiedUtc != null,
                SetupDone: steps.Count(s => s.Done),
                SetupTotal: steps.Count,
                BookingsLast30Days: bookings.GetValueOrDefault(b.Id),
                Upcoming: upcoming.GetValueOrDefault(b.Id),
                AverageStars: stars.TryGetValue(b.Id, out var average) ? Math.Round(average, 1) : null,
                ProInterest: b.ProInterestUtc != null,
                HasNote: !string.IsNullOrEmpty(b.AdminNote));
        }).ToList();
    }

    private async Task<Dictionary<Guid, SetupCounts>> SetupAsync(IQueryable<Business> businesses,
        CancellationToken cancellationToken) =>
        await businesses.AsNoTracking()
            .Select(b => new SetupCounts(
                b.Id,
                b.Services.Count(s => !s.IsDeleted),
                b.OpeningHours.Count,
                b.Images.Any(i => i.Kind == ImageKind.Logo),
                b.Description != null && b.Description != "",
                b.Images.Count(i => i.Kind == ImageKind.Galleri),
                dbContext.Bookings.Any(x => x.Service.BusinessId == b.Id)))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

    private static List<AdminSetupStep> Steps(SetupCounts counts) =>
    [
        new("Tjenester", counts.Services > 0),
        new("Åpningstider", counts.OpeningHours > 0),
        new("Logo", counts.HasLogo),
        new("Beskrivelse", counts.HasDescription),
        new("Bilder", counts.Gallery > 0),
        new("Første bestilling", counts.HasBooking)
    ];

    private sealed record SetupCounts(
        Guid Id,
        int Services,
        int OpeningHours,
        bool HasLogo,
        bool HasDescription,
        int Gallery,
        bool HasBooking);
}
