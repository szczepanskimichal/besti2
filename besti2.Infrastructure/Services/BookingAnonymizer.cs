using besti2.Application.Bookings;
using besti2.Application.Customers;
using besti2.Application.Images;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// storage: deletes the photos of old reviews (left out in tests that do not need it)
public class BookingAnonymizer(AppDbContext dbContext, IImageStorage? storage = null) : IBookingAnonymizer
{
    // Same period as promised on /personvern: the general limitation period for claims in Norway
    // (foreldelsesloven § 2), so a dispute about a visit can still be documented; no payments are stored
    public const int RetentionYears = 3;

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

            var photos = new List<string>();
            foreach (var review in oldReviews)
            {
                review.CustomerName = ProfileService.FormerCustomer;
                review.CustomerUserId = AnonymizedUserId;
                if (review.PhotoFileName is not null)
                {
                    photos.Add(review.PhotoFileName);
                    review.PhotoFileName = null;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            foreach (var photo in photos)
            {
                await (storage?.DeleteAsync(photo, cancellationToken) ?? Task.CompletedTask);
            }

            dbContext.ChangeTracker.Clear();
            count += oldReviews.Count;
        }

        // BESTI sammen invitations of those visits are deleted (the bookings above keep the history)
        while (true)
        {
            var oldInvites = await dbContext.BookingInvites
                .Where(i => i.StartUtc < cutoff)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (oldInvites.Count == 0)
            {
                break;
            }

            dbContext.BookingInvites.RemoveRange(oldInvites);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            count += oldInvites.Count;
        }

        // "Kunder": a business's note about a customer who has not booked there for 3 years
        var oldNotes = await dbContext.CustomerNotes.Where(n => n.UpdatedUtc < cutoff).ToListAsync(cancellationToken);
        foreach (var business in oldNotes.GroupBy(n => n.BusinessId))
        {
            var recent = (await dbContext.Bookings.AsNoTracking()
                    .Where(b => b.Service.BusinessId == business.Key && b.EndUtc >= cutoff)
                    .Select(b => new { b.CustomerUserId, b.CustomerPhone })
                    .ToListAsync(cancellationToken))
                .Select(b => CustomerKey.For(b.CustomerUserId, b.CustomerPhone))
                .ToHashSet();
            var gone = business.Where(n => !recent.Contains(n.CustomerUserId is not null
                ? $"k-{n.CustomerUserId}"
                : $"t-{n.CustomerPhone}")).ToList();
            dbContext.CustomerNotes.RemoveRange(gone);
            count += gone.Count;
        }

        // The admin log keeps the same 3 years
        dbContext.AdminActions.RemoveRange(await dbContext.AdminActions.Where(a => a.CreatedUtc < cutoff)
            .ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);

        return count;
    }
}
