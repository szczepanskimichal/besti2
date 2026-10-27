using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class TimeOffService(AppDbContext dbContext) : ITimeOffService
{
    private const int MaxDays = 366;

    public async Task<IReadOnlyList<TimeOffDto>> GetForBusinessAsync(Guid businessId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = SlotCalculator.ToUtc(from, TimeOnly.MinValue);
        var toUtc = SlotCalculator.ToUtc(to.AddDays(1), TimeOnly.MinValue);

        return await dbContext.TimeOffs.AsNoTracking()
            .Where(t => t.BusinessId == businessId && t.StartUtc < toUtc && t.EndUtc > fromUtc)
            .OrderBy(t => t.StartUtc)
            .Select(t => new TimeOffDto(t.Id, t.EmployeeId,
                t.Employee == null ? null : t.Employee.Name + " " + t.Employee.LastName,
                t.StartUtc, t.EndUtc, t.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreateTimeOffResult?> CreateAsync(Guid businessId, CreateTimeOffRequest request,
        CancellationToken cancellationToken = default)
    {
        string? employeeName = null;
        if (request.EmployeeId is Guid employeeId)
        {
            var employee = await dbContext.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId && !e.IsDeleted,
                    cancellationToken);
            if (employee is null)
            {
                return null;
            }

            employeeName = $"{employee.Name} {employee.LastName}";
        }

        if (request.FromDate.Year is < 2000 or > 2100 || request.ToDate.Year is < 2000 or > 2100)
        {
            return new CreateTimeOffResult(null, Error: "Ugyldig dato.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason is { Length: > 100 })
        {
            return new CreateTimeOffResult(null, Error: "Beskrivelsen kan være maks 100 tegn.");
        }

        // A whole day = from midnight to the next midnight in Oslo
        var startUtc = request.AllDay
            ? SlotCalculator.ToUtc(request.FromDate, TimeOnly.MinValue)
            : SlotCalculator.ToUtc(request.FromDate, request.FromTime);
        var endUtc = request.AllDay
            ? SlotCalculator.ToUtc(request.ToDate.AddDays(1), TimeOnly.MinValue)
            : SlotCalculator.ToUtc(request.ToDate, request.ToTime);

        if (endUtc <= startUtc)
        {
            return new CreateTimeOffResult(null, Error: "Slutt må være etter start.");
        }

        if (endUtc - startUtc > TimeSpan.FromDays(MaxDays))
        {
            return new CreateTimeOffResult(null, Error: "Du kan blokkere maks ett år om gangen.");
        }

        var timeOff = new TimeOff
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            EmployeeId = request.EmployeeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Reason = reason
        };
        dbContext.TimeOffs.Add(timeOff);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Visits already in that time stay; the business decides what to do with them
        var overlapping = await dbContext.Bookings.CountAsync(b => b.Service.BusinessId == businessId
                                                                   && (request.EmployeeId == null ||
                                                                       b.EmployeeId == request.EmployeeId)
                                                                   && b.Status != BookingStatus.Avlyst
                                                                   && b.StartUtc < endUtc && b.EndUtc > startUtc,
            cancellationToken);

        return new CreateTimeOffResult(
            new TimeOffDto(timeOff.Id, timeOff.EmployeeId, employeeName, startUtc, endUtc, reason), overlapping);
    }

    public async Task<bool> DeleteAsync(Guid businessId, Guid timeOffId, CancellationToken cancellationToken = default)
    {
        var timeOff = await dbContext.TimeOffs
            .FirstOrDefaultAsync(t => t.Id == timeOffId && t.BusinessId == businessId, cancellationToken);
        if (timeOff is null)
        {
            return false;
        }

        dbContext.TimeOffs.Remove(timeOff);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}