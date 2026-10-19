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
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return oldBookings.Count;
    }
}