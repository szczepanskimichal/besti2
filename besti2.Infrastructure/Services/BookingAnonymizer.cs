using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BookingAnonymizer(AppDbContext dbContext) : IBookingAnonymizer
{
// Same period as promised on /personvern
    public const int RetentionYears = 5;

// .invalid is a reserved domain, so no real mail can ever go there
    public const string AnonymizedEmail = "anonymisert@besti.invalid";

// A review no longer points to a person (CustomerUserId is required, so it gets this instead)
    public const string AnonymizedUserId = "anonymisert";

    public async Task<int> AnonymizeOldBookingsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var cutoff = nowUtc.AddYears(-RetentionYears);
        var oldBookings = await dbContext.Bookings
            .Where(b => b.EndUtc < cutoff && b.CustomerEmail != AnonymizedEmail)
            .ToListAsync(cancellationToken);

        foreach (var booking in oldBookings)
        {
            booking.CustomerName = "Anonymisert";
            booking.CustomerPhone = "-";
            booking.CustomerEmail = AnonymizedEmail;
            booking.MarketingConsent = false;
            booking.MarketingConsentUtc = null;
            booking.CustomerNote = null;
            booking.CustomerUserId = null;
        }

        // Reviews of those visits keep stars and text (the rating stays fair), but not the name
        var oldReviews = await dbContext.Reviews
            .Where(r => r.Booking.EndUtc < cutoff && r.CustomerName != ProfileService.FormerCustomer)
            .ToListAsync(cancellationToken);

        foreach (var review in oldReviews)
        {
            review.CustomerName = ProfileService.FormerCustomer;
            review.CustomerUserId = AnonymizedUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return oldBookings.Count + oldReviews.Count;
    }
}