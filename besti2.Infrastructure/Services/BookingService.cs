using System.Globalization;
using besti2.Application.Bookings;
using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

public class BookingService(AppDbContext dbContext, IEmailService emailService, ILogger<BookingService> logger)
    : IBookingService
{
    // date in norwegian!!!
    private static readonly TimeZoneInfo Oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private static readonly CultureInfo Norwegian = new("nb-NO");

    //
    public async Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted, cancellationToken);
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);
        if (service is null || employee is null || service.BusinessId != employee.BusinessId)
        {
            return null;
        }

        var fromUtc = date.ToDateTime(TimeOnly.MinValue).AddDays(-1);
        var toUtc = date.ToDateTime(TimeOnly.MinValue).AddDays(2);

        var busy = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId
                        && b.Status != BookingStatus.Avlyst
                        && b.StartUtc < toUtc
                        && b.EndUtc > fromUtc)
            .Select(b => new { b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);
        var hours = await dbContext.OpeningHours
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.BusinessId == service.BusinessId && h.DayOfWeek == date.DayOfWeek,
                cancellationToken);

        if (hours is null)
        {
            return [];
        }

        return SlotCalculator.GetFreeSlots(
            date,
            hours.OpensAt,
            hours.ClosesAt,
            service.DurationMinutes,
            busy.Select(b => (b.StartUtc, b.EndUtc)).ToList(),
            DateTime.UtcNow);
    }

    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, string? customerUserId = null,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error is not null)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: error);
        }

        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && !s.IsDeleted, cancellationToken);
        if (service is null)
        {
            return new CreateBookingResult(CreateBookingOutcome.NotFound);
        }

        var date = DateOnly.FromDateTime(request.StartUtc);
        var freeSlots = await GetFreeSlotsAsync(request.ServiceId, request.EmployeeId, date, cancellationToken);
        if (freeSlots is null)
        {
            return new CreateBookingResult(CreateBookingOutcome.NotFound);
        }

        if (!freeSlots.Contains(request.StartUtc))
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            EmployeeId = request.EmployeeId,
            StartUtc = request.StartUtc,
            EndUtc = request.StartUtc.AddMinutes(service.DurationMinutes),
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail.Trim().ToLowerInvariant(),
            MarketingConsent = request.MarketingConsent,
            MarketingConsentUtc = request.MarketingConsent ? DateTime.UtcNow : null,
            CustomerNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CustomerUserId = customerUserId,
            Status = BookingStatus.Venter
        };

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SendConfirmationEmailAsync(booking, service, cancellationToken);

        return new CreateBookingResult(
            CreateBookingOutcome.Created,
            new BookingCreatedDto(booking.Id, booking.StartUtc, booking.EndUtc, booking.Status.ToString()));
    }

    public async Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.EndUtc >= DateTime.UtcNow) // only future bookings!!!
            .OrderBy(b => b.StartUtc)
            .Select(b => new PanelBookingDto(
                b.Id,
                b.StartUtc,
                b.EndUtc,
                b.Status.ToString(),
                b.Service.Name,
                b.Employee.Name + " " + b.Employee.LastName,
                b.CustomerName,
                b.CustomerPhone,
                b.CustomerEmail,
                b.CustomerNote))
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<CustomerBookingDto>> GetForCustomerAsync(
        string customerUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.CustomerUserId == customerUserId)
            .OrderByDescending(b => b.StartUtc)
            .Select(b => new CustomerBookingDto(
                b.Id,
                b.StartUtc,
                b.EndUtc,
                b.Status.ToString(),
                b.Service.Name,
                b.Employee.Name + " " + b.Employee.LastName,
                b.Service.BusinessId,
                b.Service.Business.Name,
                b.Service.Business.Address + ", " + b.Service.Business.PostalCode + " " + b.Service.Business.City,
                b.ServiceId,
                b.EmployeeId))
            .ToListAsync(cancellationToken);
    }
    
    public async Task<UpdateBookingStatusOutcome> CancelByCustomerAsync(
        Guid bookingId, string customerUserId, CancellationToken cancellationToken = default)
    {
        // Only the customer's own booking; someone else's id looks like "not found"
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerUserId == customerUserId, cancellationToken);
        if (booking is null)
        {
            return UpdateBookingStatusOutcome.NotFound;
        }
        // Already cancelled or already started: too late to cancel
        if (booking.Status == BookingStatus.Avlyst || booking.StartUtc <= DateTime.UtcNow)
        {
            return UpdateBookingStatusOutcome.InvalidTransition;
        }
        booking.Status = BookingStatus.Avlyst;
        await dbContext.SaveChangesAsync(cancellationToken);
        await SendCancelledByCustomerEmailAsync(booking, cancellationToken);
        return UpdateBookingStatusOutcome.Updated;
    }

    // Update the status of a booking
    public async Task<UpdateBookingStatusOutcome> UpdateStatusAsync(
        Guid bookingId, Guid businessId, BookingStatus newStatus, CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service.BusinessId == businessId, cancellationToken);

        if (booking is null)
        {
            return UpdateBookingStatusOutcome.NotFound;
        }

        var allowed = (booking.Status, newStatus) switch
        {
            (BookingStatus.Venter, BookingStatus.Bekreftet) => true,
            (BookingStatus.Venter, BookingStatus.Avlyst) => true,
            (BookingStatus.Bekreftet, BookingStatus.Avlyst) => true,
            _ => false
        };

        if (!allowed)
        {
            return UpdateBookingStatusOutcome.InvalidTransition;
        }

        booking.Status = newStatus;
        await dbContext.SaveChangesAsync(cancellationToken);
        await SendStatusEmailAsync(booking, cancellationToken);
        return UpdateBookingStatusOutcome.Updated;
    }

    // Validate the booking request
    private static string? Validate(CreateBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 100)
        {
            return "Navn er påkrevd (maks 100 tegn).";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerPhone) || request.CustomerPhone.Length > 20)
        {
            return "Telefonnummer er påkrevd (maks 20 tegn).";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || request.CustomerEmail.Length > 250 ||
            !request.CustomerEmail.Contains('@'))
        {
            return "Oppgi en gyldig e-postadresse.";
        }

        if (request.Note is { Length: > 500 })
        {
            return "Meldingen kan være maks 500 tegn.";
        }

        return null;
    }

    // Send confirmation email to the customer; a failed email must not fail the booking
    private async Task SendConfirmationEmailAsync(Booking booking, Service service, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await dbContext.Employees.AsNoTracking()
                .FirstAsync(e => e.Id == booking.EmployeeId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);

            var start = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, Oslo);
            var when = start.ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);

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

    // Notify the customer when the business confirms or cancels; a failed email must not fail the update
    private async Task SendStatusEmailAsync(Booking booking, CancellationToken cancellationToken)
    {
        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);

            var start = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, Oslo);
            var when = start.ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);

            var (subject, message) = booking.Status == BookingStatus.Bekreftet
                ? ($"Bestillingen er bekreftet: {business.Name}", "Bestillingen din er bekreftet. Velkommen!")
                : ($"Bestillingen er avlyst: {business.Name}",
                    "Bestillingen din er dessverre avlyst. Du kan bestille en ny time på besti2.");

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
    
    // Tell the business that a customer cancelled; a failed email must not fail the cancellation
    private async Task SendCancelledByCustomerEmailAsync(Booking booking, CancellationToken cancellationToken)
    {
        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);
            var start = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, Oslo);
            var when = start.ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);
            var body =
                $"Hei {business.Name}!\n\n" +
                $"{booking.CustomerName} har avbestilt timen.\n\n" +
                $"Tjeneste: {service.Name}\n" +
                $"Tid: {when}\n\n" +
                "Tiden er nå ledig for andre.\n\n" +
                "Hilsen BESTI";
            await emailService.SendAsync(business.Email, $"Avbestilling: {service.Name}, {when}", body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send cancellation email for booking {BookingId}", booking.Id);
        }
    }
}