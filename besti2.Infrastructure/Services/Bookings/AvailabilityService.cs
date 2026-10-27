using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services.Bookings;

// Free times for the booking page: opening hours minus visits and blocked time (the maths is in SlotCalculator)
public class AvailabilityService(AppDbContext dbContext) : IAvailabilityService
{
    public async Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, int units = 1, CancellationToken cancellationToken = default)
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

        // "Per time" services can be booked for several hours in a row
        if (VisitMinutes(service, units) is not int minutes)
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
            minutes,
            busy.Select(b => (b.StartUtc, b.EndUtc)).Concat(blocked.Select(t => (t.StartUtc, t.EndUtc))).ToList(),
            DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<FreeDayDto>?> GetFreeDaysAsync(
        Guid serviceId, Guid? employeeId, DateOnly from, int days, int units = 1,
        CancellationToken cancellationToken = default)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted && !s.Business.IsHidden, cancellationToken);
        if (service is null)
        {
            return null;
        }

        if (VisitMinutes(service, units) is not int minutes)
        {
            return null;
        }

        // Outside any sensible booking range (also avoids a DateTime overflow for 9999-12-31)
        if (from.Year is < 2000 or > 2100)
        {
            return [];
        }

        days = Math.Clamp(days, 1, 14); // never more than two weeks in one request

        var employeeIds = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.BusinessId == service.BusinessId && !e.IsDeleted
                                                           && (employeeId == null || e.Id == employeeId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        // A chosen employee from another business (or deleted) = wrong link
        if (employeeId is not null && employeeIds.Count == 0)
        {
            return null;
        }

        // Same wide UTC window as GetFreeSlotsAsync, but around all the days at once
        var fromUtc = from.ToDateTime(TimeOnly.MinValue).AddDays(-1);
        var toUtc = from.ToDateTime(TimeOnly.MinValue).AddDays(days + 1);

        var busy = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.Status != BookingStatus.Avlyst
                        && b.StartUtc < toUtc
                        && b.EndUtc > fromUtc)
            .Select(b => new { b.EmployeeId, b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);
        var blocked = await dbContext.TimeOffs
            .AsNoTracking()
            .Where(t => t.BusinessId == service.BusinessId
                        && t.StartUtc < toUtc
                        && t.EndUtc > fromUtc)
            .Select(t => new { t.EmployeeId, t.StartUtc, t.EndUtc })
            .ToListAsync(cancellationToken);
        var hours = await dbContext.OpeningHours
            .AsNoTracking()
            .Where(h => h.BusinessId == service.BusinessId)
            .ToListAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        var result = new List<FreeDayDto>();

        for (var i = 0; i < days; i++)
        {
            var date = from.AddDays(i);
            var dayHours = hours.FirstOrDefault(h => h.DayOfWeek == date.DayOfWeek);
            if (dayHours is null)
            {
                result.Add(new FreeDayDto(date, [])); // closed: the day is still shown, greyed out
                continue;
            }

            var slots = employeeIds
                .SelectMany(id => SlotCalculator.GetFreeSlots(
                        date, dayHours.OpensAt, dayHours.ClosesAt, minutes,
                        busy.Where(b => b.EmployeeId == id).Select(b => (b.StartUtc, b.EndUtc))
                            .Concat(blocked.Where(t => t.EmployeeId == null || t.EmployeeId == id)
                                .Select(t => (t.StartUtc, t.EndUtc)))
                            .ToList(),
                        nowUtc)
                    .Select(start => new FreeTimeDto(start, id)))
                .OrderBy(s => s.StartUtc)
                .DistinctBy(s => s.StartUtc) // two free employees at 10:00 = one button
                .ToList();
            result.Add(new FreeDayDto(date, slots));
        }

        return result;
    }

    // How long the visit is: "Per time" services may be booked several times in a row,
    // up to 8 hours in total (null = not allowed)
    public static int? VisitMinutes(Service service, int units)
    {
        if (units == 1)
        {
            return service.DurationMinutes;
        }

        if (service.PriceType != PriceType.PerTime || units < 1 || service.DurationMinutes * units > 480)
        {
            return null;
        }

        return service.DurationMinutes * units;
    }
}