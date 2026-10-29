using System.Globalization;
using System.Text;
using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BookingExportService(AppDbContext dbContext) : IBookingExportService
{
    // Never more than about two years in one file
    public const int MaxDays = 800;

    private static readonly CultureInfo Norwegian = new("nb-NO");

    public async Task<string?> BookingsCsvAsync(Guid businessId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > MaxDays || from.Year < 2000 || to.Year > 2100)
        {
            return null;
        }

        var fromUtc = SlotCalculatorUtc(from);
        var toUtc = SlotCalculatorUtc(to.AddDays(1));
        var rows = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.StartUtc >= fromUtc && b.StartUtc < toUtc)
            .OrderBy(b => b.StartUtc)
            .Select(b => new
            {
                b.StartUtc, b.EndUtc, ServiceName = b.Service.Name,
                EmployeeName = b.Employee.Name + " " + b.Employee.LastName, b.CustomerName, b.CustomerPhone,
                b.CustomerEmail, b.Status, b.PriceNok, Online = b.CustomerUserId != null
            })
            .ToListAsync(cancellationToken);

        // Semicolons and comma decimals: what Excel expects with Norwegian settings
        var csv = new StringBuilder();
        csv.AppendLine("Dato;Fra;Til;Tjeneste;Ansatt;Kunde;Telefon;E-post;Status;Pris (kr);Bestilt");
        foreach (var row in rows)
        {
            var start = TimeZoneInfo.ConvertTimeFromUtc(row.StartUtc, SlotCalculator.Oslo);
            var end = TimeZoneInfo.ConvertTimeFromUtc(row.EndUtc, SlotCalculator.Oslo);
            csv.AppendJoin(';',
                start.ToString("dd.MM.yyyy", Norwegian),
                start.ToString("HH:mm", Norwegian),
                end.ToString("HH:mm", Norwegian),
                Cell(row.ServiceName),
                Cell(row.EmployeeName),
                Cell(row.CustomerName),
                Cell(row.CustomerPhone),
                Cell(row.CustomerEmail == BookingAnonymizer.AnonymizedEmail ? "" : row.CustomerEmail),
                StatusText(row.Status.ToString()),
                row.PriceNok?.ToString("0.00", Norwegian) ?? "",
                row.Online ? "På BESTI" : "Av bedriften");
            csv.AppendLine();
        }

        return csv.ToString();
    }

    private static DateTime SlotCalculatorUtc(DateOnly day) =>
        SlotCalculator.ToUtc(day, TimeOnly.MinValue);

    private static string StatusText(string status) => status switch
    {
        "Bekreftet" => "Bekreftet",
        "Venter" => "Venter",
        "VenterKunde" => "Venter på kunden",
        "Avlyst" => "Avlyst",
        "MotteIkke" => "Møtte ikke",
        _ => status
    };

    // Quotes when needed, and never lets Excel run a formula from a customer's name ("=HYPERLINK(...)")
    private static string Cell(string? value)
    {
        var text = value ?? "";
        if (text.Length > 0 && "=+-@\t\r".Contains(text[0]))
        {
            text = "'" + text;
        }

        return text.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }
}
