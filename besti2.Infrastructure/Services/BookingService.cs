using System.Globalization;
using besti2.Application.Bookings;
using besti2.Application.Email;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, used for "Bestill ny tid" in the e-mail after a cancellation
public class BookingService(AppDbContext dbContext, IEmailService emailService, ILogger<BookingService> logger,
    string webBaseUrl = "http://localhost:3000") : IBookingService
{
    // date in norwegian!!!
    private static readonly TimeZoneInfo Oslo = SlotCalculator.Oslo;

    private static readonly CultureInfo Norwegian = new("nb-NO");

    //
    public async Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted && !s.Business.IsHidden, cancellationToken);
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);
        if (service is null || employee is null || service.BusinessId != employee.BusinessId)
        {
            return null;
        }

        // Outside any sensible booking range (also avoids a DateTime overflow for 9999-12-31)
        if (date.Year is < 2000 or > 2100)
        {
            return [];
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
        // Blocked time of this employee or of the whole business counts as busy too
        var blocked = await dbContext.TimeOffs
            .AsNoTracking()
            .Where(t => t.BusinessId == service.BusinessId
                        && (t.EmployeeId == null || t.EmployeeId == employeeId)
                        && t.StartUtc < toUtc
                        && t.EndUtc > fromUtc)
            .Select(t => new { t.StartUtc, t.EndUtc })
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
            busy.Select(b => (b.StartUtc, b.EndUtc)).Concat(blocked.Select(t => (t.StartUtc, t.EndUtc))).ToList(),
            DateTime.UtcNow);
    }

    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, string? customerUserId = null,
        CancellationToken cancellationToken = default)
    {
        // "2026-10-22T10:00:00+02:00" arrives as local time: turn every start into real UTC first
        request = request with { StartUtc = ToUtc(request.StartUtc) };

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

        // The day in Oslo, not in UTC (00:30 on Tuesday in Oslo is still Monday in UTC)
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(request.StartUtc, Oslo));

        // Only one booking per employee is checked and saved at a time, so two clicks in the same
        // second cannot both get the slot (SQL Server lock; the in-memory test database has no locks)
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            // Someone else is booking this employee right now: the customer simply tries another time
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

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
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Second line of defence: the unique index refused a second booking for the same time
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        await SendConfirmationEmailAsync(booking, service, cancellationToken);

        return new CreateBookingResult(
            CreateBookingOutcome.Created,
            new BookingCreatedDto(booking.Id, booking.StartUtc, booking.EndUtc, booking.Status.ToString()));
    }

// The business adds a visit itself (phone call, walk-in). Confirmed at once; it may be outside the
    // opening hours, but never on top of another visit of the same employee
    public async Task<CreateBookingResult> CreateByBusinessAsync(Guid businessId, CreateManualBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateManual(request);
        if (error is not null)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: error);
        }

        // Service and employee must belong to this business
        var service = await dbContext.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.BusinessId == businessId && !s.IsDeleted,
                cancellationToken);
        var employeeExists = await dbContext.Employees.AnyAsync(
            e => e.Id == request.EmployeeId && e.BusinessId == businessId && !e.IsDeleted, cancellationToken);
        if (service is null || !employeeExists)
        {
            return new CreateBookingResult(CreateBookingOutcome.NotFound);
        }

        var startUtc = SlotCalculator.ToUtc(request.Date, request.Time);
        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        // Same protection as online bookings: one booking per employee is checked and saved at a time
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        // Blocked time (holiday, closed day) cannot be booked, not even by the business itself
        var blocked = await dbContext.TimeOffs.AnyAsync(t => t.BusinessId == businessId
                                                             && (t.EmployeeId == null ||
                                                                 t.EmployeeId == request.EmployeeId)
                                                             && t.StartUtc < endUtc && t.EndUtc > startUtc,
            cancellationToken);
        if (blocked)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid,
                Error: "Den ansatte er ikke tilgjengelig da (blokkert tid).");
        }

        var overlaps = await dbContext.Bookings.AnyAsync(b => b.EmployeeId == request.EmployeeId
                                                              && b.Status != BookingStatus.Avlyst
                                                              && b.StartUtc < endUtc && b.EndUtc > startUtc,
            cancellationToken);
        if (overlaps)
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        var email = request.CustomerEmail?.Trim().ToLowerInvariant() ?? "";
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            EmployeeId = request.EmployeeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = email,
            CustomerNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = BookingStatus.Bekreftet
        };
        dbContext.Bookings.Add(booking);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return new CreateBookingResult(CreateBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        // Only when the business asked for it and gave an e-mail address
        if (request.SendConfirmation && email.Length > 0)
        {
            await SendStatusEmailAsync(booking, null, cancellationToken);
        }

        return new CreateBookingResult(
            CreateBookingOutcome.Created,
            new BookingCreatedDto(booking.Id, booking.StartUtc, booking.EndUtc, booking.Status.ToString()));
    }

    public async Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        // Calendar: from midnight of the first day in Oslo. List: upcoming bookings and the last 14 days
        var fromUtc = from is DateOnly firstDay
            ? SlotCalculator.ToUtc(firstDay, TimeOnly.MinValue)
            : DateTime.UtcNow.AddDays(-14);
        var query = dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.EndUtc > fromUtc);
        if (to is DateOnly lastDay)
        {
            // ... until midnight after the last day (a visit is shown on every day it touches)
            var toUtc = SlotCalculator.ToUtc(lastDay.AddDays(1), TimeOnly.MinValue);
            query = query.Where(b => b.StartUtc < toUtc);
        }

        return await query
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
                b.CustomerNote,
                b.EmployeeId))
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
                b.EmployeeId,
                dbContext.Reviews.Any(r => r.BookingId == b.Id)))
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
        Guid bookingId, Guid businessId, BookingStatus newStatus, string? message = null,
        CancellationToken cancellationToken = default)
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

        // A visit that has started can still be confirmed, but no longer cancelled
        // (otherwise a business could cancel past visits to block reviews)
        if (newStatus == BookingStatus.Avlyst && booking.StartUtc <= DateTime.UtcNow)
        {
            return UpdateBookingStatusOutcome.InvalidTransition;
        }

        booking.Status = newStatus;
        await dbContext.SaveChangesAsync(cancellationToken);
// A short note from the business, only when cancelling (at most 300 characters)
        var note = newStatus == BookingStatus.Avlyst && !string.IsNullOrWhiteSpace(message)
            ? message.Trim()[..Math.Min(message.Trim().Length, 300)]
            : null;
        await SendStatusEmailAsync(booking, note, cancellationToken);        return UpdateBookingStatusOutcome.Updated;
    }

// "Flytt": another time and/or employee; never on top of another visit or blocked time,
    // outside the opening hours is allowed (as for visits the business adds itself)
    public async Task<MoveBookingResult> MoveAsync(Guid bookingId, Guid businessId, MoveBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.Service.BusinessId == businessId, cancellationToken);
        var employeeExists = await dbContext.Employees.AnyAsync(
            e => e.Id == request.EmployeeId && e.BusinessId == businessId && !e.IsDeleted, cancellationToken);
        if (booking is null || !employeeExists)
        {
            return new MoveBookingResult(MoveBookingOutcome.NotFound);
        }

        var nowUtc = DateTime.UtcNow;
        if (booking.Status == BookingStatus.Avlyst || booking.StartUtc <= nowUtc)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid,
                "Timen er avlyst eller har allerede startet, og kan ikke flyttes.");
        }

        if (request.Date.Year is < 2000 or > 2100)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid, "Ugyldig dato.");
        }

        // Same length as before (also when the service was changed since)
        var startUtc = SlotCalculator.ToUtc(request.Date, request.Time);
        var endUtc = startUtc + (booking.EndUtc - booking.StartUtc);
        if (startUtc <= nowUtc)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid, "Velg et tidspunkt fram i tid.");
        }

        if (startUtc == booking.StartUtc && request.EmployeeId == booking.EmployeeId)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid, "Velg en ny tid eller en annen ansatt.");
        }

        // The same lock as new bookings, for the employee who gets the visit
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        var blocked = await dbContext.TimeOffs.AnyAsync(t => t.BusinessId == businessId
                                                             && (t.EmployeeId == null ||
                                                                 t.EmployeeId == request.EmployeeId)
                                                             && t.StartUtc < endUtc && t.EndUtc > startUtc,
            cancellationToken);
        if (blocked)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid,
                "Den ansatte er ikke tilgjengelig da (blokkert tid).");
        }

        // Another visit of that employee (the moved visit itself does not count: 10:00 -> 10:15 is fine)
        var overlaps = await dbContext.Bookings.AnyAsync(b => b.Id != booking.Id
                                                              && b.EmployeeId == request.EmployeeId
                                                              && b.Status != BookingStatus.Avlyst
                                                              && b.StartUtc < endUtc && b.EndUtc > startUtc,
            cancellationToken);
        if (overlaps)
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        var oldStartUtc = booking.StartUtc;
        booking.EmployeeId = request.EmployeeId;
        booking.StartUtc = startUtc;
        booking.EndUtc = endUtc;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        if (request.NotifyCustomer)
        {
            await SendMovedEmailAsync(booking, oldStartUtc, cancellationToken);
        }

        return new MoveBookingResult(MoveBookingOutcome.Moved);
    }

    // "Endre tid": the customer picks another free time of the same service; the old time is freed in
    // the same step. The business confirms again, as for a new online booking
    public async Task<MoveBookingResult> ChangeTimeByCustomerAsync(Guid bookingId, string customerUserId,
        ChangeTimeRequest request, CancellationToken cancellationToken = default)
    {
        // Only the customer's own booking; someone else's id looks like "not found"
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerUserId == customerUserId, cancellationToken);
        if (booking is null)
        {
            return new MoveBookingResult(MoveBookingOutcome.NotFound);
        }
        if (booking.Status == BookingStatus.Avlyst || booking.StartUtc <= DateTime.UtcNow)
        {
            return new MoveBookingResult(MoveBookingOutcome.Invalid,
                "Timen er avbestilt eller har allerede startet, og kan ikke endres.");
        }

        var startUtc = ToUtc(request.StartUtc);
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, Oslo));

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null && !await TryLockEmployeeAsync(request.EmployeeId, cancellationToken))
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        // The same free times the customer saw (opening hours, other visits, blocked time); null = the
        // employee does not work for this business
        var freeSlots = await GetFreeSlotsAsync(booking.ServiceId, request.EmployeeId, date, cancellationToken);
        if (freeSlots is null)
        {
            return new MoveBookingResult(MoveBookingOutcome.NotFound);
        }
        if (!freeSlots.Contains(startUtc))
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        var duration = await dbContext.Services.Where(s => s.Id == booking.ServiceId)
            .Select(s => s.DurationMinutes).FirstAsync(cancellationToken);
        var oldStartUtc = booking.StartUtc;
        booking.EmployeeId = request.EmployeeId;
        booking.StartUtc = startUtc;
        booking.EndUtc = startUtc.AddMinutes(duration);
        booking.Status = BookingStatus.Venter;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return new MoveBookingResult(MoveBookingOutcome.SlotTaken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        await SendChangedByCustomerEmailAsync(booking, oldStartUtc, cancellationToken);
        return new MoveBookingResult(MoveBookingOutcome.Moved);
    }

    // Validate the booking request
    private static string? Validate(CreateBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 100)
        {
            return "Navn er påkrevd (maks 100 tegn).";
        }

        if (!IsValidPhone(request.CustomerPhone))
        {
            return "Oppgi et gyldig telefonnummer, f.eks. 912 34 567.";
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

    private static string? ValidateManual(CreateManualBookingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 100)
        {
            return "Navn er påkrevd (maks 100 tegn).";
        }

        if (!IsValidPhone(request.CustomerPhone))
        {
            return "Oppgi et gyldig telefonnummer, f.eks. 912 34 567.";
        }

        // E-mail is optional here (a walk-in customer may not give one)
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail)
            && (request.CustomerEmail.Length > 250 || !request.CustomerEmail.Contains('@')))
        {
            return "Oppgi en gyldig e-postadresse, eller la feltet stå tomt.";
        }

        if (request.Note is { Length: > 500 })
        {
            return "Notatet kan være maks 500 tegn.";
        }

        if (request.Date.Year is < 2000 or > 2100)
        {
            return "Ugyldig dato.";
        }

        return null;
    }

    // Digits, spaces and an optional + only, at least 8 digits (91234567, +47 912 34 567)
    private static bool IsValidPhone(string? value)
    {
        var phone = value?.Trim() ?? "";
        return phone.Length is > 0 and <= 20
               && phone.All(c => char.IsDigit(c) || c == ' ' || c == '+')
               && phone.Count(char.IsDigit) >= 8
               && phone.LastIndexOf('+') <= 0;
    }

    // SQL Server lock per employee inside the open transaction; false when not granted within 10 s
    private async Task<bool> TryLockEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var lockName = $"booking-{employeeId}";
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DECLARE @result int; EXEC @result = sp_getapplock @Resource = {lockName}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Booking lock not granted', 1;",
                cancellationToken);
            return true;
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            return false;
        }
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

    // The new time after "Flytt"; a failed email must not undo the move
    private async Task SendMovedEmailAsync(Booking booking, DateTime oldStartUtc, CancellationToken cancellationToken)
    {
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

            string When(DateTime utc) =>
                TimeZoneInfo.ConvertTimeFromUtc(utc, Oslo).ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);

            var body =
                $"Hei {booking.CustomerName}!\n\n" +
                "Timen din har fått ny tid.\n\n" +
                $"Ny tid: {When(booking.StartUtc)}\n" +
                $"Tidligere tid: {When(oldStartUtc)}\n" +
                $"Tjeneste: {service.Name} hos {employee.Name} {employee.LastName}\n" +
                $"Sted: {business.Name}, {business.Address}, {business.PostalCode} {business.City}\n\n" +
                "Passer ikke den nye tiden? Ta kontakt med bedriften, eller avbestill i Min Besti.\n\n" +
                "Hilsen BESTI";

            await emailService.SendAsync(booking.CustomerEmail, $"Ny tid for bestillingen: {business.Name}", body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send moved email for booking {BookingId}", booking.Id);
        }
    }

    private async Task SendStatusEmailAsync(Booking booking, string? note, CancellationToken cancellationToken)
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

            var start = TimeZoneInfo.ConvertTimeFromUtc(booking.StartUtc, Oslo);
            var when = start.ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);
            
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

    // Tell the business that a customer picked a new time; a failed email must not undo the change
    private async Task SendChangedByCustomerEmailAsync(Booking booking, DateTime oldStartUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var service = await dbContext.Services.AsNoTracking()
                .FirstAsync(s => s.Id == booking.ServiceId, cancellationToken);
            var business = await dbContext.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == service.BusinessId, cancellationToken);
            var employee = await dbContext.Employees.AsNoTracking()
                .FirstAsync(e => e.Id == booking.EmployeeId, cancellationToken);

            string When(DateTime utc) =>
                TimeZoneInfo.ConvertTimeFromUtc(utc, Oslo).ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);

            var body =
                $"Hei {business.Name}!\n\n" +
                $"{booking.CustomerName} har endret tid.\n\n" +
                $"Ny tid: {When(booking.StartUtc)} hos {employee.Name} {employee.LastName}\n" +
                $"Tidligere tid: {When(oldStartUtc)}\n" +
                $"Tjeneste: {service.Name}\n\n" +
                "Den gamle tiden er ledig igjen. Bekreft den nye tiden i kalenderen.\n\n" +
                "Hilsen BESTI";
            await emailService.SendAsync(business.Email, $"Endret tid: {service.Name}, {When(booking.StartUtc)}",
                body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send changed-time email for booking {BookingId}", booking.Id);
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

    // Local -> UTC; a time without a zone is taken as UTC (that is what the web app sends)
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}
