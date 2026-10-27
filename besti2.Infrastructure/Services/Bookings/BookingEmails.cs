using besti2.Application.Common;
using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Bookings;

// All e-mails about a visit. A failed e-mail is only logged: it must never undo the booking change
// webBaseUrl = address of the Next.js site, for the links in the e-mails (Bestill ny tid, answer to a new time)
public class BookingEmails(
    AppDbContext dbContext,
    IEmailService emailService,
    ILogger logger,
    string webBaseUrl)
{
    // Send confirmation email to the customer; a failed email must not fail the booking
    public async Task SendConfirmationAsync(Booking booking, Service service, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await dbContext.Employees.AsNoTracking()
                .FirstAsync(e => e.Id == booking.EmployeeId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);

            var when = OsloTime.Format(booking.StartUtc);

            var body =
                $"Hei {booking.CustomerName}!\n\n" +
                $"Vi har mottatt bestillingen din hos {business.Name}.\n\n" +
                $"Tjeneste: {service.Name}\n" +
                $"Hos: {employee.Name} {employee.LastName}\n" +
                $"Tid: {when}\n" +
                $"Adresse: {business.Address}, {business.PostalCode} {business.City}\n\n" +
                "Bedriften bekrefter bestillingen så snart som mulig.\n\n" +
                "Hilsen BESTI";

            await emailService.SendAsync(booking.CustomerEmail, $"Bestilling mottatt: {business.Name}", body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send confirmation email for booking {BookingId}", booking.Id);
        }
    }

    // The new time after "Flytt"; a failed email must not undo the move
    // answerToken = the customer must accept the new time (Godta / Passer ikke on the BESTI page)
    public async Task SendMovedAsync(Booking booking, DateTime oldStartUtc, string? answerToken,
        CancellationToken cancellationToken)
    {
        var answerLink = answerToken is null ? null : $"{webBaseUrl}/ny-tid?token={answerToken}";
        if (booking.CustomerEmail.Length == 0)
        {
            return;
        }

        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);
            var employee = await dbContext.Employees.AsNoTracking()
                .FirstAsync(e => e.Id == booking.EmployeeId, cancellationToken);

            var body =
                $"Hei {booking.CustomerName}!\n\n" +
                (answerLink is null ? "Timen din har fått ny tid.\n\n" : "Bedriften ønsker å flytte timen din.\n\n") +
                $"Ny tid: {OsloTime.Format(booking.StartUtc)}\n" +
                $"Tidligere tid: {OsloTime.Format(oldStartUtc)}\n" +
                $"Tjeneste: {service.Name} hos {employee.Name} {employee.LastName}\n" +
                $"Sted: {business.Name}, {business.Address}, {business.PostalCode} {business.City}\n\n" +
                (answerLink is null
                    ? "Passer ikke den nye tiden? Ta kontakt med bedriften, eller avbestill i Min Besti.\n\n"
                    : $"Passer den nye tiden? Svar her: {answerLink}\n\n") +
                "Hilsen BESTI";

            var subject = answerLink is null
                ? $"Ny tid for bestillingen: {business.Name}"
                : $"Passer den nye tiden? {business.Name}";
            await emailService.SendAsync(booking.CustomerEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send moved email for booking {BookingId}", booking.Id);
        }
    }

    // Bekreftet or Avlyst by the business, to the customer
    public async Task SendStatusAsync(Booking booking, string? note, CancellationToken cancellationToken)
    {
        // A visit added by the business may have no e-mail address
        if (booking.CustomerEmail.Length == 0)
        {
            return;
        }

        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);

            var when = OsloTime.Format(booking.StartUtc);

            // Cancelled: the business's own words (if any) and a link straight to its booking page
            var (subject, message) = booking.Status == BookingStatus.Bekreftet
                ? ($"Bestillingen er bekreftet: {business.Name}", "Bestillingen din er bekreftet. Velkommen!")
                : ($"Bestillingen er avlyst: {business.Name}",
                    "Bestillingen din er dessverre avlyst.\n\n" +
                    (note is null ? "" : $"Melding fra {business.Name}: {note}\n\n") +
                    $"Bestill ny tid: {webBaseUrl}/bedrift/{business.Id}");
            var body =
                $"Hei {booking.CustomerName}!\n\n" +
                $"{message}\n\n" +
                $"Tjeneste: {service.Name}\n" +
                $"Tid: {when}\n" +
                $"Sted: {business.Name}, {business.Address}, {business.PostalCode} {business.City}\n\n" +
                "Hilsen BESTI";

            await emailService.SendAsync(booking.CustomerEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send status email for booking {BookingId}", booking.Id);
        }
    }

    // "Møtte ikke": how to object (the business first, then BESTI); a failed e-mail must not undo the status
    public async Task SendNoShowAsync(Booking booking, CancellationToken cancellationToken)
    {
        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);
            var when = OsloTime.Format(booking.StartUtc);
            var body =
                $"Hei {booking.CustomerName}!\n\n" +
                $"{business.Name} har registrert at du ikke møtte til timen din.\n\n" +
                $"Tjeneste: {service.Name}\n" +
                $"Tid: {when}\n\n" +
                $"Var du der likevel? Kontakt {business.Name} på {business.PhoneNumber}, " +
                "eller skriv til post@bestiapp.no, så ser vi på det.\n\n" +
                "Hilsen BESTI";
            await emailService.SendAsync(booking.CustomerEmail, $"Registrert som ikke møtt: {business.Name}", body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send no-show email for booking {BookingId}", booking.Id);
        }
    }

    // Tell the business whether the customer accepted the new time; a failed email must not undo the answer
    public async Task SendNewTimeAnswerAsync(Booking booking, bool accept, CancellationToken cancellationToken)
    {
        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);
            var when = OsloTime.Format(booking.StartUtc);
            var body =
                $"Hei {business.Name}!\n\n" +
                (accept
                    ? $"{booking.CustomerName} har godtatt den nye tiden. Bekreft timen i kalenderen.\n\n"
                    : $"{booking.CustomerName} kan ikke komme på den nye tiden. Timen er avlyst, og tiden er ledig igjen.\n\n") +
                $"Tjeneste: {service.Name}\n" +
                $"Tid: {when}\n\n" +
                "Hilsen BESTI";
            var subject =
                accept ? $"Ny tid godtatt: {service.Name}, {when}" : $"Ny tid avslått: {service.Name}, {when}";
            await SendToOwnerAsync(business.Id, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send new-time answer email for booking {BookingId}", booking.Id);
        }
    }

    // Tell the business that a customer cancelled; a failed email must not fail the cancellation
    public async Task SendCancelledByCustomerAsync(Booking booking, string? note,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);
            var when = OsloTime.Format(booking.StartUtc);
            var body =
                $"Hei {business.Name}!\n\n" +
                $"{booking.CustomerName} har avbestilt timen.\n\n" +
                $"Tjeneste: {service.Name}\n" +
                $"Tid: {when}\n\n" +
                (note is null ? "" : $"Melding fra kunden: {note}\n\n") +
                "Tiden er nå ledig for andre.\n\n" +
                "Hilsen BESTI";
            await SendToOwnerAsync(business.Id, $"Avbestilling: {service.Name}, {when}", body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send cancellation email for booking {BookingId}", booking.Id);
        }
    }

    // Mails to the business go to the owner's confirmed login e-mail, never to the public contact e-mail
    // (nobody has confirmed that one, a typo would send customer data to a stranger)
    private async Task SendToOwnerAsync(Guid businessId, string subject, string body,
        CancellationToken cancellationToken)
    {
        var ownerEmail = await dbContext.Users
            .Where(u => u.BusinessId == businessId && u.EmailConfirmed)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (ownerEmail is not null)
        {
            await emailService.SendAsync(ownerEmail, subject, body, cancellationToken);
        }
    }
}