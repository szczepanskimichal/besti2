using besti2.Application.Images;
using besti2.Application.Reviews;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// images/storage: for the photos on reviews (left out in tests that do not need them)
public class ReviewService(AppDbContext dbContext, IBusinessImageService? images = null, IImageStorage? storage = null)
    : IReviewService
{
    private const int MaxLength = 500;

    // A visit can be reviewed for 30 days; after that it disappears from Min Besti
    public const int ReviewDays = 30;

    public async Task<string?> CreateAsync(string customerUserId, CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Stars is < 1 or > 5)
        {
            return "Velg mellom 1 og 5 stjerner.";
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        if (comment is { Length: > MaxLength })
        {
            return "Kommentaren kan være maks 500 tegn.";
        }

        // Only the customer's own booking; someone else's looks like "not found"
        var booking = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.Id == request.BookingId && b.CustomerUserId == customerUserId)
            .Select(b => new { b.Id, b.EndUtc, b.Status, b.CustomerName, b.Service.BusinessId })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
        {
            return "Fant ikke bestillingen.";
        }

        // Every customer may review a visit that is over and was not cancelled, also when the business
        // never pressed "Bekreft": the business must not choose who gets to review it
        if (booking.Status == BookingStatus.MotteIkke)
        {
            return "Du kan ikke vurdere en time du ikke møtte til.";
        }

        if (booking.Status == BookingStatus.Avlyst || booking.EndUtc > DateTime.UtcNow)
        {
            return "Du kan gi en vurdering etter timen.";
        }

        if (booking.EndUtc < DateTime.UtcNow.AddDays(-ReviewDays))
        {
            return "Du kan gi en vurdering innen 30 dager etter timen.";
        }

        if (await dbContext.Reviews.AnyAsync(r => r.BookingId == booking.Id, cancellationToken))
        {
            return "Du har allerede vurdert denne timen.";
        }

        dbContext.Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            BusinessId = booking.BusinessId,
            CustomerUserId = customerUserId,
            CustomerName = booking.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
                           "Kunde",
            Stars = request.Stars,
            Comment = comment,
            CreatedUtc = DateTime.UtcNow
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Double click on "Send": the first one was saved, the second hit the unique index
            return "Du har allerede vurdert denne timen.";
        }

        // The bell in the business panel
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            BusinessId = booking.BusinessId,
            Text = $"Ny vurdering: {request.Stars} av 5 stjerner fra {booking.CustomerName.Split(' ')[0]}",
            Link = "/panel/vurderinger",
            CreatedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> SetPhotoAsync(string customerUserId, Guid bookingId, Stream content, long length,
        CancellationToken cancellationToken = default)
    {
        if (images is null || storage is null)
        {
            return "Bilder er ikke tilgjengelig.";
        }

        // Only the customer's own review, while the visit can still be reviewed
        var review = await dbContext.Reviews
            .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.CustomerUserId == customerUserId,
                cancellationToken);
        if (review is null)
        {
            return "Fant ikke vurderingen.";
        }

        if (review.CreatedUtc < DateTime.UtcNow.AddDays(-ReviewDays))
        {
            return "Du kan legge til bilde innen 30 dager.";
        }

        var (fileName, error) = await images.SaveReviewPhotoAsync(content, length, cancellationToken);
        if (fileName is null)
        {
            return error;
        }

        var oldFile = review.PhotoFileName;
        review.PhotoFileName = fileName;
        await dbContext.SaveChangesAsync(cancellationToken);
        // The old file only after the database points to the new one
        if (oldFile is not null)
        {
            await storage.DeleteAsync(oldFile, cancellationToken);
        }

        return null;
    }

    public async Task<BusinessReviewsDto> GetForBusinessAsync(Guid businessId, bool includeHidden = false,
        string? viewerUserId = null, int? take = null, CancellationToken cancellationToken = default)
    {
        // The stars and the count from every visible review, counted by the database
        var visible = dbContext.Reviews.Where(r => r.BusinessId == businessId && r.HiddenUtc == null);
        var count = await visible.CountAsync(cancellationToken);
        double? average = count == 0
            ? null
            : Math.Round(await visible.AverageAsync(r => (double)r.Stars, cancellationToken), 1);

        IQueryable<Review> query = dbContext.Reviews.AsNoTracking()
            .Where(r => r.BusinessId == businessId && (includeHidden || r.HiddenUtc == null))
            .OrderByDescending(r => r.CreatedUtc)
            .ThenBy(r => r.Id);
        if (take is int limit)
        {
            query = query.Take(limit);
        }

        var rows = await query
            .Select(r => new
            {
                r.Id, r.Stars, r.Comment, r.CustomerName, r.CreatedUtc, r.BusinessReply, r.PhotoFileName,
                r.HiddenUtc, r.HiddenReason,
                Helpful = dbContext.ReviewHelpfuls.Count(h => h.ReviewId == r.Id),
                Marked = viewerUserId != null && dbContext.ReviewHelpfuls.Any(h => h.ReviewId == r.Id
                    && h.UserId == viewerUserId)
            })
            .ToListAsync(cancellationToken);

        var reviews = rows.Select(r => new ReviewDto(r.Id, r.Stars, r.Comment, r.CustomerName, r.CreatedUtc,
                r.BusinessReply,
                PhotoUrl: r.PhotoFileName is null ? null : storage?.GetUrl(r.PhotoFileName),
                Helpful: r.Helpful,
                MarkedHelpful: r.Marked,
                Hidden: r.HiddenUtc != null,
                HiddenReason: r.HiddenReason))
            .ToList();

        return new BusinessReviewsDto(average, count, reviews);
    }

    public async Task<HelpfulResult?> ToggleHelpfulAsync(string userId, Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await dbContext.Reviews.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.HiddenUtc == null, cancellationToken);
        // A customer cannot vote for their own review
        if (review is null || review.CustomerUserId == userId)
        {
            return null;
        }

        var existing = await dbContext.ReviewHelpfuls
            .FirstOrDefaultAsync(h => h.ReviewId == reviewId && h.UserId == userId, cancellationToken);
        if (existing is null)
        {
            dbContext.ReviewHelpfuls.Add(new ReviewHelpful
            {
                Id = Guid.NewGuid(), ReviewId = reviewId, UserId = userId, CreatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.ReviewHelpfuls.Remove(existing);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Double click: the first one counted
        }

        var count = await dbContext.ReviewHelpfuls.CountAsync(h => h.ReviewId == reviewId, cancellationToken);
        return new HelpfulResult(existing is null, count);
    }

    public async Task<string?> ReplyAsync(Guid businessId, Guid reviewId, string reply,
        CancellationToken cancellationToken = default)
    {
        var text = reply?.Trim() ?? "";

        if (text.Length == 0)
        {
            return "Skriv et svar.";
        }

        if (text.Length > MaxLength)
        {
            return "Svaret kan være maks 500 tegn.";
        }

        // Only reviews of the business's own bookings
        var review = await dbContext.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.BusinessId == businessId, cancellationToken);

        if (review is null)
        {
            return "Fant ikke vurderingen.";
        }

        review.BusinessReply = text;

        review.RepliedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }
}