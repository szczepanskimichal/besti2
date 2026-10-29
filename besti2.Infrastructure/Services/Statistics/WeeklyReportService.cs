using System.Globalization;
using System.Text;
using besti2.Application.Bookings;
using besti2.Application.Email;
using besti2.Application.Statistics;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services.Statistics;

// "Ukerapport": every Monday morning a short e-mail to the owner with last week's numbers and one tip.
// A service message to the business about its own account (not marketing); it can be turned off in Statistikk
public class WeeklyReportService(
    AppDbContext dbContext,
    IBusinessStatsService statsService,
    IEmailService emailService,
    ILogger<WeeklyReportService> logger,
    string webBaseUrl) : IWeeklyReportService
{
    // Monday from 07:00 in Oslo; Tuesday too, in case the API was down on Monday. Never at night
    private const int FirstHour = 7;
    private const int LastHour = 20;

    private static readonly CultureInfo Norwegian = new("nb-NO");

    public async Task<int> SendDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var oslo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SlotCalculator.Oslo);
        if (oslo.DayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Tuesday) || oslo.Hour is < FirstHour or > LastHour)
        {
            return 0;
        }

        var weekStartUtc = BusinessStatsService.WeekStartUtc(nowUtc);
        var lastWeekStartUtc = weekStartUtc.AddDays(-7);
        // Approved before this week, visible and not sent yet this week
        var due = await dbContext.Businesses
            .Where(b => !b.IsHidden && !b.WeeklyReportOff && b.ApprovedUtc != null && b.ApprovedUtc < weekStartUtc
                        && (b.WeeklyReportSentUtc == null || b.WeeklyReportSentUtc < weekStartUtc))
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var business in due)
        {
            try
            {
                var ownerEmail = await dbContext.Users.Where(u => u.BusinessId == business.Id)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync(cancellationToken);
                var stats = await statsService.GetForPeriodAsync(business.Id, lastWeekStartUtc, weekStartUtc, nowUtc,
                    cancellationToken);
                if (ownerEmail is not null && stats is not null)
                {
                    var monday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(lastWeekStartUtc, SlotCalculator.Oslo));
                    await emailService.SendAsync(ownerEmail,
                        $"Ukerapport for {business.Name}: {Visits(stats.Current.Visits)} forrige uke",
                        Body(business.Name, monday, stats), cancellationToken);
                    sent++;
                }

                // Marked also without an owner, so it is not looked at again every hour
                business.WeeklyReportSentUtc = nowUtc;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The next hour tries again
                logger.LogError(ex, "Ukerapport for business {BusinessId} failed", business.Id);
            }
        }

        return sent;
    }

    public string Body(string businessName, DateOnly monday, BusinessStatsDto stats)
    {
        var now = stats.Current;
        var before = stats.Previous;
        var text = new StringBuilder();
        text.Append("Hei!\n\n");
        text.Append($"Slik gikk uke {ISOWeek.GetWeekOfYear(monday.ToDateTime(TimeOnly.MinValue))} for {businessName} ")
            .Append($"({monday.ToString("d. MMMM", Norwegian)} – {monday.AddDays(6).ToString("d. MMMM", Norwegian)}):\n\n");
        text.Append($"Besøk: {now.Visits} (uka før: {before.Visits})\n");
        text.Append($"Beregnet omsetning: {Kroner(now.Revenue)} (uka før: {Kroner(before.Revenue)})");
        text.Append(now.VisitsWithoutPrice > 0 ? $", pluss {Visits(now.VisitsWithoutPrice)} uten fast pris\n" : "\n");
        text.Append($"Nye kunder: {now.NewCustomers}, faste kunder: {now.ReturningCustomers}\n");
        text.Append($"Avbestilt: {now.Cancelled}, møtte ikke: {now.NoShows}\n");
        if (stats.NewReviews > 0)
        {
            text.Append($"Nye vurderinger: {stats.NewReviews} (snitt {stats.AverageStars?.ToString("0.0", Norwegian)} av 5)\n");
        }

        if (stats.Services.Count > 0)
        {
            text.Append($"Mest populær: {stats.Services[0].Name} ({stats.Services[0].Visits})\n");
        }

        text.Append($"\nDenne uka har du {(stats.UpcomingVisits == 1 ? "1 time" : $"{stats.UpcomingVisits} timer")} booket");
        text.Append(stats.UpcomingRevenue > 0 ? $" ({Kroner(stats.UpcomingRevenue)}).\n" : ".\n");
        if (stats.Tips.FirstOrDefault(t => t.Kind == "tips") is { } tip)
        {
            text.Append($"\nUkens tips: {tip.Title}. {tip.Text}\n");
        }

        text.Append($"\nSe mer i Statistikk: {webBaseUrl}/panel/statistikk\n\n");
        text.Append("Omsetningen er beregnet ut fra prisene i BESTI. Vil du ikke ha ukerapporten, " +
                    "kan du slå den av i Statistikk.\n\n");
        text.Append("Hilsen BESTI");
        return text.ToString();
    }

    private static string Visits(int count) => count == 1 ? "1 besøk" : $"{count} besøk";

    // "7 450 kr" (a normal space, the e-mail is plain text)
    private static string Kroner(decimal amount) =>
        Math.Round(amount).ToString("#,0", Norwegian).Replace(' ', ' ') + " kr";
}
