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

    // Old data is changed in small portions, so a big first run never holds thousands of rows in memory
    public const int BatchSize = 1000;

    public async Task<int> AnonymizeOldBookingsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // A night job may wait longer for the database than a visitor (default is 30 s)
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        }

        var cutoff = nowUtc.AddYears(-RetentionYears);
        var count = 0;
        while (true)
        {
            var oldBookings = await dbContext.Bookings
                .Where(b => b.EndUtc < cutoff && b.CustomerEmail != AnonymizedEmail)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (oldBookings.Count == 0)
            {
                break;
            }

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

            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            count += oldBookings.Count;
        }

        // Reviews of those visits keep stars and text (the rating stays fair), but not the name
        while (true)
        {
            var oldReviews = await dbContext.Reviews
                .Where(r => r.Booking.EndUtc < cutoff && r.CustomerName != ProfileService.FormerCustomer)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (oldReviews.Count == 0)
            {
                break;
            }

            foreach (var review in oldReviews)
            {
                review.CustomerName = ProfileService.FormerCustomer;
                review.CustomerUserId = AnonymizedUserId;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            count += oldReviews.Count;
        }

        return count;
    }
}
