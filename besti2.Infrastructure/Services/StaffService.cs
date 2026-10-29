using besti2.Application.Bookings;
using besti2.Application.Common;
using besti2.Application.Customers;
using besti2.Application.Email;
using besti2.Application.Staff;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, for the link in the invitation
public class StaffService(AppDbContext dbContext, IEmailService emailService, string webBaseUrl = "http://localhost:3000")
    : IStaffService
{
    // The invitation link works for a week
    public const int InviteDays = 7;

    // /ansatt shows at most two weeks at a time
    public const int MaxDays = 14;

    public async Task<string?> InviteAsync(Guid businessId, Guid employeeId, string? email,
        CancellationToken cancellationToken = default)
    {
        var address = email?.Trim().ToLowerInvariant() ?? "";
        if (address.Length is < 3 or > 250 || !address.Contains('@') || address.Contains(' '))
        {
            return "Skriv en gyldig e-postadresse.";
        }

        var employee = await dbContext.Employees.Include(e => e.Business)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId && !e.IsDeleted,
                cancellationToken);
        if (employee is null)
        {
            return "Fant ikke den ansatte.";
        }

        // A business account logs in to the panel, never to /ansatt (one login, one role)
        var normalized = address.ToUpperInvariant();
        if (await dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalized && u.BusinessId != null,
                cancellationToken))
        {
            return "Denne e-posten brukes til en bedriftskonto. Bruk den ansattes egen e-post.";
        }

        // The same person cannot be two employees in one business
        if (await dbContext.Employees.AnyAsync(e => e.BusinessId == businessId && e.Id != employeeId && !e.IsDeleted
                                                    && e.StaffEmail == address, cancellationToken))
        {
            return "En annen ansatt har allerede fått tilgang med denne e-posten.";
        }

        var token = SecureToken.New();
        employee.StaffEmail = address;
        employee.StaffUserId = null;
        employee.StaffInviteTokenHash = SecureToken.Hash(token);
        employee.StaffInviteExpiresUtc = DateTime.UtcNow.AddDays(InviteDays);
        await dbContext.SaveChangesAsync(cancellationToken);

        var link = $"{webBaseUrl}/ansatt/invitasjon?kode={Uri.EscapeDataString(token)}";
        var body =
            $"Hei {employee.Name}!\n\n" +
            $"{employee.Business.Name} har gitt deg tilgang til din egen kalender på BESTI. " +
            "Der ser du timene dine, hvem som kommer og notatene om kundene.\n\n" +
            $"Åpne lenken og logg inn eller lag en gratis konto med denne e-posten ({address}):\n{link}\n\n" +
            $"Lenken virker i {InviteDays} dager.\n\n" +
            "Hilsen BESTI";
        await emailService.SendAsync(address, $"Din kalender hos {employee.Business.Name}", body, cancellationToken);
        return null;
    }

    public async Task<bool> RemoveAsync(Guid businessId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        Clear(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StaffAccessDto>> GetAccessAsync(Guid businessId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Employees.AsNoTracking()
            .Where(e => e.BusinessId == businessId && !e.IsDeleted && e.StaffEmail != null)
            .Select(e => new StaffAccessDto(e.Id, e.StaffEmail, e.StaffUserId != null, e.StaffInviteExpiresUtc))
            .ToListAsync(cancellationToken);

    public async Task<StaffInviteInfoDto?> GetInviteAsync(string token, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var employee = await FindInviteAsync(token, nowUtc, cancellationToken);
        return employee is null
            ? null
            : new StaffInviteInfoDto(employee.Business.Name, employee.Name, employee.StaffEmail!);
    }

    public async Task<string?> AcceptAsync(string userId, string token, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var employee = await FindInviteAsync(token, nowUtc, cancellationToken);
        if (employee is null)
        {
            return "Lenken er ugyldig eller utløpt. Be bedriften sende en ny.";
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.BusinessId is not null)
        {
            return "Logg inn med en vanlig BESTI-konto (ikke bedriftskontoen).";
        }

        // Only the person the e-mail went to
        if (!string.Equals(user.Email, employee.StaffEmail, StringComparison.OrdinalIgnoreCase))
        {
            return $"Invitasjonen gjelder {employee.StaffEmail}. Logg inn med den e-posten.";
        }

        employee.StaffUserId = user.Id;
        employee.StaffInviteTokenHash = null;
        employee.StaffInviteExpiresUtc = null;
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            BusinessId = employee.BusinessId,
            Text = $"{employee.Name} har koblet til sin egen kalender",
            Link = "/panel/ansatte",
            CreatedUtc = nowUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public Task<bool> IsStaffAsync(string userId, CancellationToken cancellationToken = default) =>
        dbContext.Employees.AnyAsync(e => e.StaffUserId == userId && !e.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<StaffPlaceDto>> GetScheduleAsync(string userId, DateOnly from, int days,
        CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, MaxDays);
        if (from.Year is < 2000 or > 2100)
        {
            return [];
        }

        var employees = await dbContext.Employees.AsNoTracking()
            .Where(e => e.StaffUserId == userId && !e.IsDeleted)
            .Select(e => new { e.Id, e.BusinessId, e.Name, e.LastName, BusinessName = e.Business.Name, e.Business.Slug })
            .ToListAsync(cancellationToken);
        var fromUtc = SlotCalculator.ToUtc(from, TimeOnly.MinValue);
        var toUtc = SlotCalculator.ToUtc(from.AddDays(days), TimeOnly.MinValue);
        var startsAfterUtc = fromUtc.AddMinutes(-BookingRules.MaxVisitMinutes);
        var cards = new CustomerCardService(dbContext);

        var places = new List<StaffPlaceDto>();
        foreach (var employee in employees)
        {
            var bookings = await dbContext.Bookings.AsNoTracking()
                .Where(b => b.EmployeeId == employee.Id && b.Status != BookingStatus.Avlyst
                                                        && b.StartUtc > startsAfterUtc && b.StartUtc < toUtc
                                                        && b.EndUtc > fromUtc)
                .OrderBy(b => b.StartUtc)
                .Select(b => new
                {
                    b.Id, b.StartUtc, b.EndUtc, b.Status, ServiceName = b.Service.Name, b.CustomerName,
                    b.CustomerPhone, b.CustomerNote, b.CustomerUserId
                })
                .ToListAsync(cancellationToken);
            var notes = await cards.NotesAsync(employee.BusinessId,
                bookings.Select(b => CustomerKey.For(b.CustomerUserId, b.CustomerPhone)).OfType<string>(),
                cancellationToken);
            var blocks = await dbContext.TimeOffs.AsNoTracking()
                .Where(t => t.BusinessId == employee.BusinessId
                            && (t.EmployeeId == null || t.EmployeeId == employee.Id)
                            && t.StartUtc < toUtc && t.EndUtc > fromUtc)
                .OrderBy(t => t.StartUtc)
                .Select(t => new StaffBlockDto(t.StartUtc, t.EndUtc, t.Reason))
                .ToListAsync(cancellationToken);
            places.Add(new StaffPlaceDto(employee.Id, employee.BusinessName, employee.Slug,
                $"{employee.Name} {employee.LastName}",
                bookings.Select(b => new StaffBookingDto(b.Id, b.StartUtc, b.EndUtc, b.Status.ToString(),
                    b.ServiceName, b.CustomerName, b.CustomerPhone, b.CustomerNote,
                    CustomerKey.For(b.CustomerUserId, b.CustomerPhone) is string key
                    && notes.TryGetValue(key, out var note)
                        ? note
                        : null)).ToList(),
                blocks));
        }

        return places;
    }

    public async Task<bool> LeaveAsync(string userId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.StaffUserId == userId, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        Clear(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Employee?> FindInviteAsync(string token, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 100)
        {
            return null;
        }

        var hash = SecureToken.Hash(token);
        return await dbContext.Employees.Include(e => e.Business)
            .FirstOrDefaultAsync(e => e.StaffInviteTokenHash == hash && e.StaffInviteExpiresUtc > nowUtc
                                                                     && !e.IsDeleted, cancellationToken);
    }

    private static void Clear(Employee employee)
    {
        employee.StaffEmail = null;
        employee.StaffUserId = null;
        employee.StaffInviteTokenHash = null;
        employee.StaffInviteExpiresUtc = null;
    }
}
