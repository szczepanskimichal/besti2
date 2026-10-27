using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Bookings;

// Messages in the bell (Varsler) about a visit
public class BookingNotifier(AppDbContext dbContext, ILogger logger)
{
    // A message in the bell. To the business, or to the customer when the booking belongs to an account.
    // In a text for the customer, {0} is replaced by the business's name. A failure must not fail the booking
    public async Task NotifyAsync(Booking booking, bool toBusiness, string text, string? link,
        CancellationToken cancellationToken)
    {
        if (!toBusiness && booking.CustomerUserId is null)
        {
            return; // a walk-in without an account has no bell
        }

        try
        {
            var business = await dbContext.Services.AsNoTracking()
                .Where(s => s.Id == booking.ServiceId)
                .Select(s => new { s.BusinessId, s.Business.Name })
                .FirstAsync(cancellationToken);
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                BusinessId = toBusiness ? business.BusinessId : null,
                UserId = toBusiness ? null : booking.CustomerUserId,
                BookingId = booking.Id,
                Text = Shorten(text.Replace("{0}", business.Name)),
                // Cancelled visits are gone from Min Besti: the customer can book again with the business
                Link = link ?? $"/bedrift/{business.BusinessId}",
                CreatedUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not save notification for booking {BookingId}", booking.Id);
        }
    }

    // The column holds 300 characters (a long cancellation note is cut)
    private static string Shorten(string text) => text.Length <= 300 ? text : text[..299] + "…";

    // The visit in the business calendar: its day with the details window open
    public static string PanelLink(Booking booking)
    {
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, SlotCalculator.Oslo));
        return $"/panel?dag={day:yyyy-MM-dd}&bestilling={booking.Id}";
    }
}