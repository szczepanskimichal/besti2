using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services.Bookings;

public class RebookReminderService(AppDbContext dbContext) : IRebookReminderService
{
    // "Velg selv" on the confirmation page: any whole number of weeks, at most half a year
    public const int MinWeeks = 1;
    public const int MaxWeeks = 26;

    // Never at night: only from 9 to 20 in Oslo (the job runs every hour)
    private const int FirstHour = 9;
    private const int LastHour = 20;

    public async Task<string?> SetAsync(string userId, SetRebookReminderRequest request, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.RebookReminders
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ServiceId == request.ServiceId, cancellationToken);
        if (request.Weeks == 0)
        {
            if (existing is not null)
            {
                dbContext.RebookReminders.Remove(existing);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return null;
        }
        if (request.Weeks is < MinWeeks or > MaxWeeks)
        {
            return "Velg hvor mange uker.";
        }

        var service = await dbContext.Services
            .Where(s => s.Id == request.ServiceId && !s.IsDeleted)
            .Select(s => new { s.Id, s.BusinessId })
            .FirstOrDefaultAsync(cancellationToken);
        if (service is null)
        {
            return "Fant ikke tjenesten.";
        }
        var employeeId = request.EmployeeId is Guid id
                         && await dbContext.Employees.AnyAsync(e => e.Id == id && e.BusinessId == service.BusinessId,
                             cancellationToken)
            ? request.EmployeeId
            : null;

        if (existing is null)
        {
            existing = new RebookReminder
            {
                Id = Guid.NewGuid(), UserId = userId, ServiceId = service.Id, CreatedUtc = nowUtc
            };
            dbContext.RebookReminders.Add(existing);
        }
        existing.Weeks = request.Weeks;
        existing.EmployeeId = employeeId;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two clicks at the same moment: the first one saved it
        }

        return null;
    }

    public async Task<IReadOnlyList<RebookReminderDto>> GetMineAsync(string userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.RebookReminders
            .Where(r => r.UserId == userId && !r.Service.IsDeleted)
            .OrderBy(r => r.Service.Business.Name)
            .Select(r => new RebookReminderDto(r.Id, r.Service.Business.Name, r.Service.Business.Slug, r.ServiceId,
                r.Service.Name, r.EmployeeId, r.Weeks))
            .ToListAsync(cancellationToken);

    public async Task<bool> RemoveAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var reminder = await dbContext.RebookReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
        if (reminder is null)
        {
            return false;
        }
        dbContext.RebookReminders.Remove(reminder);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SendDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var hour = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SlotCalculator.Oslo).Hour;
        if (hour is < FirstHour or > LastHour)
        {
            return 0;
        }

        var reminders = await dbContext.RebookReminders
            .Include(r => r.Service).ThenInclude(s => s.Business)
            .ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var reminder in reminders)
        {
            var service = reminder.Service;
            if (service.IsDeleted)
            {
                dbContext.RebookReminders.Remove(reminder);
                continue;
            }

            // The last visit that really happened (not cancelled, not "Møtte ikke")
            var last = await dbContext.Bookings
                .Where(b => b.CustomerUserId == reminder.UserId && b.ServiceId == reminder.ServiceId
                            && b.Status == BookingStatus.Bekreftet && b.EndUtc <= nowUtc)
                .OrderByDescending(b => b.EndUtc)
                .Select(b => new { b.Id, b.EndUtc })
                .FirstOrDefaultAsync(cancellationToken);
            if (last is null || last.Id == reminder.LastRemindedBookingId
                             || last.EndUtc.AddDays(7 * reminder.Weeks) > nowUtc)
            {
                continue;
            }

            // Already booked again with this business: no reminder needed (the next visit starts a new count)
            var bookedAgain = await dbContext.Bookings.AnyAsync(b => b.CustomerUserId == reminder.UserId
                    && b.Service.BusinessId == service.BusinessId && b.StartUtc > nowUtc
                    && b.Status != BookingStatus.Avlyst,
                cancellationToken);
            reminder.LastRemindedBookingId = last.Id;
            if (bookedAgain || service.Business.IsHidden)
            {
                continue;
            }

            var link = $"/{service.Business.Slug}/bestill?tjeneste={service.Id}"
                       + (reminder.EmployeeId is Guid employeeId ? $"&ansatt={employeeId}" : "");
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = reminder.UserId,
                Text = $"Tid for ny time hos {service.Business.Name}? Det er {reminder.Weeks} uker siden {service.Name}. " +
                       "Trykk her for å se ledige tider.",
                Link = link,
                CreatedUtc = nowUtc
            });
            sent++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return sent;
    }
}
