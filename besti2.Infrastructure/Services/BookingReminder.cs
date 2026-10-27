using System.Globalization;
using besti2.Application.Bookings;
using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, for the Min Besti link in the e-mail
public class BookingReminder(
    AppDbContext dbContext,
    IEmailService emailService,
    ILogger<BookingReminder> logger,
    string webBaseUrl = "http://localhost:3000") : IBookingReminder
{
    // The reminder goes about a day before the visit
    public const int HoursBefore = 24;

    // Booked less than 12 hours before the visit: the booking e-mail is fresh enough
    public const int MinHoursBooked = 12;

    private static readonly TimeZoneInfo Oslo = SlotCalculator.Oslo;
    private static readonly CultureInfo Norwegian = new("nb-NO");

    public async Task<int> SendRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var until = nowUtc.AddHours(HoursBefore);
        // Only confirmed visits: Venter, Avlyst and Møtte ikke get nothing, and a walk-in without e-mail cannot get it
        var bookings = await dbContext.Bookings
            .Include(b => b.Service).ThenInclude(s => s.Business)
            .Include(b => b.Employee)
            .Where(b => b.Status == BookingStatus.Bekreftet
                        && b.ReminderSentUtc == null
                        && b.CustomerEmail != ""
                        && b.StartUtc > nowUtc
                        && b.StartUtc <= until)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var booking in bookings.Where(b => b.StartUtc - b.CreatedUtc >= TimeSpan.FromHours(MinHoursBooked)))
        {
            try
            {
                await emailService.SendAsync(booking.CustomerEmail, Subject(booking, nowUtc), Body(booking, nowUtc),
                    cancellationToken);
                // Saved after each mail, so a crash halfway never sends the same reminder twice
                booking.ReminderSentUtc = nowUtc;
                await dbContext.SaveChangesAsync(cancellationToken);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failed mail must not stop the others; this one is tried again on the next run
                logger.LogWarning(ex, "Could not send reminder for booking {BookingId}", booking.Id);
            }
        }

        return sent;
    }

    // "Påminnelse: time i morgen kl. 10:00 hos Klipp & Stil"
    private static string Subject(Booking booking, DateTime nowUtc)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, Oslo);
        var today = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Oslo).Date;
        var day = start.Date == today ? "i dag" : "i morgen";
        return $"Påminnelse: time {day} kl. {start:HH:mm} hos {booking.Service.Business.Name}";
    }

    private string Body(Booking booking, DateTime nowUtc)
    {
        var business = booking.Service.Business;
        return
            $"Hei {booking.CustomerName}!\n\n" +
            $"Husk timen din hos {business.Name}.\n\n" +
            $"Tjeneste: {booking.Service.Name}\n" +
            $"Hos: {booking.Employee.Name} {booking.Employee.LastName}\n" +
            $"Tid: {When(booking.StartUtc)}\n" +
            $"Adresse: {business.Address}, {business.PostalCode} {business.City}\n\n" +
            $"{CancelRule(booking, business, nowUtc)}\n\n" +
            // Visits added by the business may have no customer account, so no Min Besti for them
            (booking.CustomerUserId is null ? "" : $"Se timen din i Min Besti: {webBaseUrl}/min-besti\n\n") +
            "Velkommen!\n" +
            "Hilsen BESTI";
    }

    // The same free-cancel rule as in Min Besti (Business.FreeCancelHours)
    private static string CancelRule(Booking booking, Business business, DateTime nowUtc)
    {
        if (business.FreeCancelHours == 0)
        {
            return "Kan du ikke komme? Du kan avbestille gratis helt frem til timen.";
        }

        var deadline = booking.StartUtc.AddHours(-business.FreeCancelHours);
        return deadline > nowUtc
            ? $"Kan du ikke komme? Gratis avbestilling inntil {business.FreeCancelHours} timer før timen, " +
              $"altså innen {When(deadline)}."
            : $"Kan du ikke komme? Fristen for gratis avbestilling har gått ut, så ring {business.Name} " +
              $"på {business.PhoneNumber}.";
    }

    // "torsdag 22. oktober kl. 10:00"
    private static string When(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, Oslo).ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);
}
