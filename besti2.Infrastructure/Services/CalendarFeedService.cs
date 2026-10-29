using System.Security.Cryptography;
using System.Text;
using besti2.Application.Businesses;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class CalendarFeedService(AppDbContext dbContext) : ICalendarFeedService
{
    // Last month (to look back at) and the next half year; cancelled visits are left out
    private static readonly TimeSpan Back = TimeSpan.FromDays(31);
    private static readonly TimeSpan Ahead = TimeSpan.FromDays(183);

    public async Task<string?> GetTokenAsync(Guid businessId, CancellationToken cancellationToken = default) =>
        await dbContext.Businesses.Where(b => b.Id == businessId).Select(b => b.CalendarFeedToken)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<string> CreateTokenAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.FirstAsync(b => b.Id == businessId, cancellationToken);
        // 32 random bytes: nobody can guess it, and it fits in a link (no + / =)
        business.CalendarFeedToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        await dbContext.SaveChangesAsync(cancellationToken);
        return business.CalendarFeedToken;
    }

    public async Task TurnOffAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.FirstAsync(b => b.Id == businessId, cancellationToken);
        business.CalendarFeedToken = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetCalendarAsync(string token, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }
        var business = await dbContext.Businesses.Where(b => b.CalendarFeedToken == token)
            .Select(b => new { b.Id, b.Name, b.Address, b.PostalCode, b.City })
            .FirstOrDefaultAsync(cancellationToken);
        if (business is null)
        {
            return null;
        }

        var from = nowUtc - Back;
        var to = nowUtc + Ahead;
        var bookings = await dbContext.Bookings
            .Where(b => b.Service.BusinessId == business.Id && b.Status != BookingStatus.Avlyst
                        && b.EndUtc >= from && b.StartUtc <= to)
            .OrderBy(b => b.StartUtc)
            .Select(b => new
            {
                b.Id, b.StartUtc, b.EndUtc, b.Status, b.CustomerName, b.CustomerPhone, b.CustomerNote,
                ServiceName = b.Service.Name, EmployeeName = b.Employee.Name
            })
            .ToListAsync(cancellationToken);

        var location = $"{business.Address}, {business.PostalCode} {business.City}";
        var ics = new StringBuilder();
        Line(ics, "BEGIN:VCALENDAR");
        Line(ics, "VERSION:2.0");
        Line(ics, "PRODID:-//BESTI//Kalender//NO");
        Line(ics, "CALSCALE:GREGORIAN");
        Line(ics, "METHOD:PUBLISH");
        Line(ics, $"X-WR-CALNAME:{Escape($"BESTI – {business.Name}")}");
        Line(ics, "X-WR-TIMEZONE:Europe/Oslo");
        // How often the calendar app should fetch again (Apple and Outlook listen, Google decides itself)
        Line(ics, "REFRESH-INTERVAL;VALUE=DURATION:PT15M");
        Line(ics, "X-PUBLISHED-TTL:PT15M");
        foreach (var booking in bookings)
        {
            var waiting = booking.Status is BookingStatus.Venter or BookingStatus.VenterKunde;
            var prefix = booking.Status switch
            {
                BookingStatus.Venter => "Venter: ",
                BookingStatus.VenterKunde => "Ny tid foreslått: ",
                BookingStatus.MotteIkke => "Møtte ikke: ",
                _ => ""
            };
            var description = $"Hos {booking.EmployeeName}\nTelefon: {booking.CustomerPhone}";
            if (!string.IsNullOrWhiteSpace(booking.CustomerNote))
            {
                description += $"\nMelding: {booking.CustomerNote}";
            }

            Line(ics, "BEGIN:VEVENT");
            Line(ics, $"UID:{booking.Id}@bestiapp.no");
            Line(ics, $"DTSTAMP:{IcsDate(nowUtc)}");
            Line(ics, $"DTSTART:{IcsDate(booking.StartUtc)}");
            Line(ics, $"DTEND:{IcsDate(booking.EndUtc)}");
            Line(ics, $"SUMMARY:{Escape($"{prefix}{booking.ServiceName} – {booking.CustomerName}")}");
            Line(ics, $"DESCRIPTION:{Escape(description)}");
            Line(ics, $"LOCATION:{Escape(location)}");
            Line(ics, $"STATUS:{(waiting ? "TENTATIVE" : "CONFIRMED")}");
            Line(ics, "END:VEVENT");
        }
        Line(ics, "END:VCALENDAR");
        return ics.ToString();
    }

    private static string IcsDate(DateTime utc) => utc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

    // Backslashes, commas, semicolons and new lines must be escaped in iCalendar text
    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace(";", "\\;")
        .Replace(",", "\\,").Replace("\r\n", "\\n").Replace("\n", "\\n");

    // iCalendar lines are at most 75 bytes; longer ones go on with a space at the start of the next line.
    // Never cut inside a letter like "ø" (two bytes)
    private static void Line(StringBuilder ics, string line)
    {
        var bytes = 0;
        var start = 0;
        for (var i = 0; i < line.Length; i++)
        {
            var size = Encoding.UTF8.GetByteCount(line.AsSpan(i, char.IsHighSurrogate(line[i]) ? 2 : 1));
            if (bytes + size > (start == 0 ? 75 : 74))
            {
                ics.Append(line, start, i - start).Append("\r\n ");
                start = i;
                bytes = 0;
            }
            bytes += size;
            if (char.IsHighSurrogate(line[i]))
            {
                i++;
            }
        }
        ics.Append(line, start, line.Length - start).Append("\r\n");
    }
}
