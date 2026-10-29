using besti2.Application.Bookings;
using besti2.Application.Invites;
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
        // Visits are max 8 hours, so older ones cannot reach into the window (keeps the index read short)
        var startsAfterUtc = fromUtc.AddMinutes(-BookingRules.MaxVisitMinutes);

        var busy = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId
                        && b.Status != BookingStatus.Avlyst
                        && b.StartUtc < toUtc
                        && b.StartUtc > startsAfterUtc
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
            BookingRules.EarliestStartUtc(DateTime.UtcNow),
            hours.BreakStart,
            hours.BreakEnd);
    }

    public async Task<IReadOnlyList<FreeDayDto>?> GetFreeDaysAsync(
        Guid serviceId, Guid? employeeId, DateOnly from, int days, int units = 1, int persons = 1,
        CancellationToken cancellationToken = default)
    {
        var free = await FreeTimesAsync(serviceId, employeeId, from, days, units, persons, cancellationToken);
        // One button per time; the chosen employee (or the first free one) gets the booking
        return free?.Select(day => new FreeDayDto(day.Date,
                day.Times.Select(t => new FreeTimeDto(t.StartUtc, employeeId ?? t.EmployeeIds[0])).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>?> GetFreeEmployeesAsync(
        Guid serviceId, Guid? employeeId, DateTime startUtc, int units, int persons,
        CancellationToken cancellationToken = default)
    {
        // The day in Oslo, not in UTC (00:30 on Tuesday in Oslo is still Monday in UTC)
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, SlotCalculator.Oslo));
        var free = await FreeTimesAsync(serviceId, employeeId, date, 1, units, persons, cancellationToken);
        if (free is null)
        {
            return null;
        }

        var time = free[0].Times.FirstOrDefault(t => t.StartUtc == startUtc);
        if (time.EmployeeIds is null)
        {
            return [];
        }

        // The chosen employee first, then the others in their usual order
        return time.EmployeeIds
            .OrderBy(id => id == employeeId ? 0 : 1)
            .Take(persons)
            .ToList();
    }

    // Free start times day by day, each with every employee free then (at least `persons` of them)
    private async Task<List<(DateOnly Date, IReadOnlyList<(DateTime StartUtc, IReadOnlyList<Guid> EmployeeIds)> Times)>?>
        FreeTimesAsync(Guid serviceId, Guid? employeeId, DateOnly from, int days, int units, int persons,
            CancellationToken cancellationToken)
    {
        if (persons < 1 || persons > InviteRules.MaxPersons)
        {
            return null;
        }

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
            .Where(e => e.BusinessId == service.BusinessId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        // A chosen employee from another business (or deleted) = wrong link
        if (employeeId is not null && !employeeIds.Contains(employeeId.Value))
        {
            return null;
        }

        // Alone with a chosen employee: only that employee's times. For BESTI sammen the others count too
        if (employeeId is not null && persons == 1)
        {
            employeeIds = [employeeId.Value];
        }

        // Same wide UTC window as GetFreeSlotsAsync, but around all the days at once
        var fromUtc = from.ToDateTime(TimeOnly.MinValue).AddDays(-1);
        var toUtc = from.ToDateTime(TimeOnly.MinValue).AddDays(days + 1);
        // Visits are max 8 hours, so older ones cannot reach into the window (keeps the index read short)
        var startsAfterUtc = fromUtc.AddMinutes(-BookingRules.MaxVisitMinutes);

        var busy = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.Status != BookingStatus.Avlyst
                        && b.StartUtc < toUtc
                        && b.StartUtc > startsAfterUtc
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
        // Times starting within the next hour are not shown (see BookingRules)
        var earliestUtc = BookingRules.EarliestStartUtc(DateTime.UtcNow);
        var result = new List<(DateOnly, IReadOnlyList<(DateTime, IReadOnlyList<Guid>)>)>();

        for (var i = 0; i < days; i++)
        {
            var date = from.AddDays(i);
            var dayHours = hours.FirstOrDefault(h => h.DayOfWeek == date.DayOfWeek);
            if (dayHours is null)
            {
                result.Add((date, [])); // closed: the day is still shown, greyed out
                continue;
            }

            var freeByEmployee = employeeIds.Select(id => (id, SlotCalculator.GetFreeSlots(
                date, dayHours.OpensAt, dayHours.ClosesAt, minutes,
                busy.Where(b => b.EmployeeId == id).Select(b => (b.StartUtc, b.EndUtc))
                    .Concat(blocked.Where(t => t.EmployeeId == null || t.EmployeeId == id)
                        .Select(t => (t.StartUtc, t.EndUtc)))
                    .ToList(),
                earliestUtc, dayHours.BreakStart, dayHours.BreakEnd)));
            var times = SlotCalculator.FreeForPersons(freeByEmployee, persons)
                // BESTI sammen with a chosen employee: that employee must be one of the free ones
                .Where(t => employeeId is null || t.EmployeeIds.Contains(employeeId.Value))
                .ToList();
            result.Add((date, times));
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