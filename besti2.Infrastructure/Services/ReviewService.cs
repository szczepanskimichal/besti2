using besti2.Application.Reviews;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class ReviewService(AppDbContext dbContext) : IReviewService
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

    public async Task<BusinessReviewsDto> GetForBusinessAsync(Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await dbContext.Reviews.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedUtc)
            .Select(r => new ReviewDto(r.Id, r.Stars, r.Comment, r.CustomerName, r.CreatedUtc, r.BusinessReply))
            .ToListAsync(cancellationToken);

        double? average = reviews.Count == 0 ? null : Math.Round(reviews.Average(r => r.Stars), 1);

        return new BusinessReviewsDto(average, reviews.Count, reviews);
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