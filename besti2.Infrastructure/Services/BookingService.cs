using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BookingService(AppDbContext dbContext) : IBookingService
{
    public async Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

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

        return SlotCalculator.GetFreeSlots(
            date,
            service.DurationMinutes,
            busy.Select(b => (b.StartUtc, b.EndUtc)).ToList(),
            DateTime.UtcNow);
    }

        public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error is not null)
        {
            return new CreateBookingResult(CreateBookingOutcome.Invalid, Error: error);
        }

        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
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
            Status = BookingStatus.Venter
        };

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateBookingResult(
            CreateBookingOutcome.Created,
            new BookingCreatedDto(booking.Id, booking.StartUtc, booking.EndUtc, booking.Status.ToString()));
    }

    public async Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId)
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
                b.CustomerEmail))
            .ToListAsync(cancellationToken);
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

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || request.CustomerEmail.Length > 250 || !request.CustomerEmail.Contains('@'))
        {
            return "Oppgi en gyldig e-postadresse.";
        }

        return null;
    }
}