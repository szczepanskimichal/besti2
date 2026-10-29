using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Bookings;

public class WaitlistService(AppDbContext dbContext, IAvailabilityService availability, ILogger<WaitlistService> logger)
    : IWaitlistService
{
    public const int MaxPerCustomer = 5;
    public const int MaxDaysAhead = 60;

    public async Task<string?> JoinAsync(string userId, JoinWaitlistRequest request, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var today = OsloDay(nowUtc);
        if (request.Date < today || request.Date > today.AddDays(MaxDaysAhead))
        {
            return $"Velg en dag fra i dag og inntil {MaxDaysAhead} dager fram.";
        }

        var service = await dbContext.Services
            .Where(s => s.Id == request.ServiceId && !s.IsDeleted && !s.Business.IsHidden)
            .Select(s => new { s.Id, s.BusinessId })
            .FirstOrDefaultAsync(cancellationToken);
        if (service is null)
        {
            return "Fant ikke tjenesten.";
        }
        if (request.EmployeeId is Guid employeeId
            && !await dbContext.Employees.AnyAsync(e => e.Id == employeeId && e.BusinessId == service.BusinessId,
                cancellationToken))
        {
            return "Fant ikke den ansatte.";
        }

        var mine = await dbContext.WaitlistEntries
            .Where(w => w.UserId == userId && w.NotifiedUtc == null && w.Date >= today)
            .ToListAsync(cancellationToken);
        if (mine.Any(w => w.ServiceId == request.ServiceId && w.EmployeeId == request.EmployeeId && w.Date == request.Date))
        {
            return null; // already waiting for exactly this
        }
        if (mine.Count >= MaxPerCustomer)
        {
            return $"Du kan stå på venteliste for høyst {MaxPerCustomer} dager om gangen. Fjern en i Min Besti først.";
        }

        // What is free right now is not news: only times that become free later are
        var free = await FreeTimesAsync(request.ServiceId, request.EmployeeId, request.Date, cancellationToken);
        dbContext.WaitlistEntries.Add(new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = request.ServiceId,
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            CreatedUtc = nowUtc,
            KnownFreeTimes = Join(free ?? [])
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<WaitlistEntryDto>> GetMineAsync(string userId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var today = OsloDay(nowUtc);
        var entries = await dbContext.WaitlistEntries
            .Where(w => w.UserId == userId && w.Date >= today)
            .OrderBy(w => w.Date)
            .Select(w => new
            {
                w.Id, w.Service.Business.Name, w.Service.Business.Slug, w.ServiceId, ServiceName = w.Service.Name,
                w.EmployeeId, w.Date, w.NotifiedUtc
            })
            .ToListAsync(cancellationToken);
        var employeeIds = entries.Where(e => e.EmployeeId != null).Select(e => e.EmployeeId!.Value).ToList();
        var employees = await dbContext.Employees.Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
        return entries.Select(e => new WaitlistEntryDto(e.Id, e.Name, e.Slug, e.ServiceId, e.ServiceName, e.EmployeeId,
                e.EmployeeId is Guid id ? employees.GetValueOrDefault(id) : null, e.Date, e.NotifiedUtc != null))
            .ToList();
    }

    public async Task<bool> LeaveAsync(string userId, Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == entryId && w.UserId == userId, cancellationToken);
        if (entry is null)
        {
            return false;
        }
        dbContext.WaitlistEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CheckAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var today = OsloDay(nowUtc);
        dbContext.WaitlistEntries.RemoveRange(await dbContext.WaitlistEntries
            .Where(w => w.Date < today).ToListAsync(cancellationToken));

        var waiting = await dbContext.WaitlistEntries
            .Include(w => w.Service).ThenInclude(s => s.Business)
            .Where(w => w.NotifiedUtc == null && w.Date >= today)
            .ToListAsync(cancellationToken);
        var told = 0;
        // Everybody waiting for the same thing is checked with one look at the calendar
        foreach (var group in waiting.GroupBy(w => (w.ServiceId, w.EmployeeId, w.Date)))
        {
            var free = await FreeTimesAsync(group.Key.ServiceId, group.Key.EmployeeId, group.Key.Date,
                cancellationToken);
            if (free is null)
            {
                dbContext.WaitlistEntries.RemoveRange(group); // the service or the business is gone
                continue;
            }

            foreach (var entry in group)
            {
                var known = entry.KnownFreeTimes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                var news = free.Where(t => !known.Contains(Key(t))).ToList();
                entry.KnownFreeTimes = Join(free);
                if (news.Count == 0)
                {
                    continue;
                }

                // First come, first served: everybody waiting gets the message at the same time
                entry.NotifiedUtc = nowUtc;
                told++;
                var service = entry.Service;
                var time = TimeZoneInfo.ConvertTimeFromUtc(news[0], SlotCalculator.Oslo);
                var day = entry.Date == today ? "i dag" : entry.Date == today.AddDays(1) ? "i morgen"
                    : entry.Date.ToString("dddd d. MMMM", new System.Globalization.CultureInfo("nb-NO"));
                var link = $"/{service.Business.Slug}/bestill?tjeneste={service.Id}&fra={entry.Date:yyyy-MM-dd}" +
                           $"&dato={entry.Date:yyyy-MM-dd}" + (entry.EmployeeId is Guid e ? $"&ansatt={e}" : "");
                dbContext.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = entry.UserId,
                    Text = $"Det ble ledig hos {service.Business.Name} {day} kl. {time:HH:mm} ({service.Name}). " +
                           "Først til mølla!",
                    Link = link,
                    CreatedUtc = nowUtc
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (told > 0)
        {
            logger.LogInformation("Waitlist: {Count} customers told about a free time", told);
        }
        return told;
    }

    // All free start times that day; null = the service (or the business) no longer exists
    private async Task<List<DateTime>?> FreeTimesAsync(Guid serviceId, Guid? employeeId, DateOnly date,
        CancellationToken cancellationToken)
    {
        var days = await availability.GetFreeDaysAsync(serviceId, employeeId, date, 1,
            cancellationToken: cancellationToken);
        return days?.FirstOrDefault()?.Slots.Select(s => s.StartUtc).Distinct().OrderBy(t => t).ToList()
               ?? (days is null ? null : []);
    }

    private static string Key(DateTime utc) => utc.ToString("yyyyMMddHHmm");

    private static string Join(IEnumerable<DateTime> times) => string.Join(',', times.Select(Key));

    private static DateOnly OsloDay(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, SlotCalculator.Oslo));
}
