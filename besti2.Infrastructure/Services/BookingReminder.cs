using besti2.Application.Bookings;
using besti2.Application.Common;
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

    // A mail server that keeps failing: stop after 3 tries (45 minutes), not every 15 minutes until the visit
    public const int MaxAttempts = 3;

    // A booking still waiting for "Bekreft" this long after it was made: the business gets one reminder
    public const int PendingAfterHours = 2;

    private static readonly TimeZoneInfo Oslo = SlotCalculator.Oslo;

    public async Task<int> SendRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var until = nowUtc.AddHours(HoursBefore);
        // Only confirmed visits: Venter, Avlyst and Møtte ikke get nothing, and a walk-in without e-mail cannot get it
        var bookings = await dbContext.Bookings
            .Include(b => b.Service).ThenInclude(s => s.Business)
            .Include(b => b.Employee)
            .Where(b => b.Status == BookingStatus.Bekreftet
                        && b.ReminderSentUtc == null
                        && b.ReminderAttempts < MaxAttempts
                        && b.CustomerEmail != ""
                        && !b.NoReminder
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
                booking.ReminderAttempts++;
                logger.LogWarning(ex, "Could not send reminder for booking {BookingId} (attempt {Attempt} of {Max})",
                    booking.Id, booking.ReminderAttempts, MaxAttempts);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return sent;
    }

    public async Task<int> SendPendingRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var madeBefore = nowUtc.AddHours(-PendingAfterHours);
        var pending = await dbContext.Bookings
            .Include(b => b.Service)
            .Include(b => b.Employee)
            .Where(b => b.Status == BookingStatus.Venter
                        && b.PendingReminderSentUtc == null
                        && b.StartUtc > nowUtc
                        && b.CreatedUtc <= madeBefore)
            .OrderBy(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        var sent = 0;
        // One e-mail per business with all its waiting bookings
        foreach (var group in pending.GroupBy(b => b.Service.BusinessId))
        {
            var ownerEmail = await dbContext.Users
                .Where(u => u.BusinessId == group.Key && u.EmailConfirmed)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(cancellationToken);
            try
            {
                if (ownerEmail is not null)
                {
                    var count = group.Count();
                    var lines = group.Select(b =>
                        $"- {OsloTime.Format(b.StartUtc)}: {b.Service.Name}, {b.CustomerName} " +
                        $"(hos {b.Employee.Name} {b.Employee.LastName})");
                    var body =
                        "Hei!\n\n" +
                        (count == 1
                            ? "Denne bestillingen venter fortsatt på bekreftelse:\n\n"
                            : $"Disse {count} bestillingene venter fortsatt på bekreftelse:\n\n") +
                        string.Join("\n", lines) + "\n\n" +
                        $"Bekreft eller avlys dem her: {webBaseUrl}/panel/liste\n\n" +
                        "Kunden vet ikke om timen gjelder før dere har bekreftet den.\n" +
                        "Vil dere slippe dette? Velg «Bekreft automatisk» under Profil i panelet.\n\n" +
                        "Hilsen BESTI";
                    await emailService.SendAsync(ownerEmail,
                        count == 1
                            ? "1 bestilling venter på bekreftelse"
                            : $"{count} bestillinger venter på bekreftelse", body, cancellationToken);
                    sent++;
                }

                // Also without an owner e-mail: nobody to remind, so these are never looked at again
                foreach (var booking in group)
                {
                    booking.PendingReminderSentUtc = nowUtc;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Tried again on the next run; the other businesses still get theirs
                logger.LogWarning(ex, "Could not send the pending bookings reminder to business {BusinessId}",
                    group.Key);
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
            $"Tid: {OsloTime.Format(booking.StartUtc)}\n" +
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
              $"altså innen {OsloTime.Format(deadline)}."
            : $"Kan du ikke komme? Fristen for gratis avbestilling har gått ut, så ring {business.Name} " +
              $"på {business.PhoneNumber}.";
    }
}