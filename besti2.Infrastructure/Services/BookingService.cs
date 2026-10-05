using besti2.Application.Bookings;
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
}