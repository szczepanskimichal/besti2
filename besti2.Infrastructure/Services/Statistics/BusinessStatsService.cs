using besti2.Application.Bookings;
using besti2.Application.Statistics;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services.Statistics;

// Counted in memory from the business's bookings: a busy salon has a few thousand a year, a small query
public class BusinessStatsService(AppDbContext dbContext) : IBusinessStatsService
{
    public static readonly int[] DayOptions = [7, 30, 90, 365];

    private const int WeeksInChart = 12;

    // Tips about a low share or rate only when there is enough to say something
    private const int MinVisitsForRates = 10;

    private static readonly string[] WeekdayNames =
        ["mandag", "tirsdag", "onsdag", "torsdag", "fredag", "lørdag", "søndag"];

    public Task<BusinessStatsDto?> GetAsync(Guid businessId, int days, DateTime nowUtc,
        CancellationToken cancellationToken = default) =>
        DayOptions.Contains(days)
            ? GetForPeriodAsync(businessId, nowUtc.AddDays(-days), nowUtc, nowUtc, cancellationToken)
            : Task.FromResult<BusinessStatsDto?>(null);

    public async Task<BusinessStatsDto?> GetForPeriodAsync(Guid businessId, DateTime fromUtc, DateTime toUtc,
        DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => new
            {
                b.AutoConfirm,
                b.FreeCancelHours,
                b.WeeklyReportOff,
                b.ProInterestUtc,
                HasDescription = b.Description != null && b.Description != "",
                HasCalendarFeed = b.CalendarFeedToken != null,
                HasLoyalty = b.LoyaltyVisits != null
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (business is null)
        {
            return null;
        }

        var length = toUtc - fromUtc;
        var previousFromUtc = fromUtc - length;
        var chartFromUtc = WeekStartUtc(nowUtc).AddDays(-7 * (WeeksInChart - 1));
        var loadFromUtc = new[] { previousFromUtc, chartFromUtc }.Min();

        var bookings = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.StartUtc >= loadFromUtc)
            .Select(b => new Visit(b.StartUtc, b.EndUtc, b.Status, b.PriceNok, b.CustomerUserId, b.CustomerEmail,
                b.CustomerPhone, b.Service.Name, b.Employee.Name + " " + b.Employee.LastName))
            .ToListAsync(cancellationToken);

        // When each customer first came (all time), to tell new customers from returning ones
        var firstVisits = (await dbContext.Bookings.AsNoTracking()
                .Where(b => b.Service.BusinessId == businessId && b.Status == BookingStatus.Bekreftet
                                                                && b.EndUtc <= nowUtc)
                .Select(b => new { b.CustomerUserId, b.CustomerEmail, b.CustomerPhone, b.StartUtc })
                .ToListAsync(cancellationToken))
            .GroupBy(b => CustomerKey(b.CustomerUserId, b.CustomerEmail, b.CustomerPhone))
            .ToDictionary(g => g.Key, g => g.Min(b => b.StartUtc));

        var current = Numbers(bookings, fromUtc, toUtc, nowUtc, firstVisits);
        var previous = Numbers(bookings, previousFromUtc, fromUtc, nowUtc, firstVisits);
        var done = bookings.Where(b => IsDone(b, nowUtc) && b.StartUtc >= fromUtc && b.StartUtc < toUtc).ToList();

        var upcoming = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.StartUtc >= nowUtc
                        && b.StartUtc < nowUtc.AddDays(7)
                        && (b.Status == BookingStatus.Bekreftet || b.Status == BookingStatus.Venter
                                                                || b.Status == BookingStatus.VenterKunde))
            .Select(b => b.PriceNok)
            .ToListAsync(cancellationToken);

        var reviews = await dbContext.Reviews.AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.HiddenUtc == null)
            .Select(r => new { r.Stars, r.CreatedUtc })
            .ToListAsync(cancellationToken);

        var weekdays = new int[7];
        var hours = new int[24];
        foreach (var visit in done)
        {
            var oslo = TimeZoneInfo.ConvertTimeFromUtc(visit.StartUtc, SlotCalculator.Oslo);
            weekdays[((int)oslo.DayOfWeek + 6) % 7]++;
            hours[oslo.Hour]++;
        }

        var weeks = Enumerable.Range(0, WeeksInChart)
            .Select(i =>
            {
                var startUtc = chartFromUtc.AddDays(7 * i);
                var week = bookings.Where(b => IsDone(b, nowUtc) && b.StartUtc >= startUtc
                                                                 && b.StartUtc < startUtc.AddDays(7)).ToList();
                return new StatsWeek(DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, SlotCalculator.Oslo)),
                    week.Count, week.Sum(b => b.PriceNok ?? 0));
            })
            .ToList();

        var openingHours = await dbContext.OpeningHours.AsNoTracking()
            .Where(o => o.BusinessId == businessId)
            .Select(o => new { o.DayOfWeek, o.OpensAt, o.ClosesAt, o.BreakStart, o.BreakEnd })
            .ToListAsync(cancellationToken);
        var employees = await dbContext.Employees.CountAsync(e => e.BusinessId == businessId && !e.IsDeleted,
            cancellationToken);
        var occupancy = Occupancy(bookings, fromUtc, toUtc, nowUtc,
            openingHours.ToDictionary(o => o.DayOfWeek, o => (o.ClosesAt - o.OpensAt).TotalMinutes
                                                          - ((o.BreakEnd - o.BreakStart)?.TotalMinutes ?? 0)), employees);

        var setup = new Setup(
            HasOpeningHours: openingHours.Count > 0,
            Services: await dbContext.Services.CountAsync(s => s.BusinessId == businessId && !s.IsDeleted,
                cancellationToken),
            Images: await dbContext.BusinessImages.CountAsync(i => i.BusinessId == businessId && i.Kind == ImageKind.Galleri,
                cancellationToken),
            business.HasDescription,
            business.HasCalendarFeed,
            business.AutoConfirm,
            business.FreeCancelHours,
            Employees: employees,
            business.HasLoyalty);

        return new BusinessStatsDto(
            Days: (int)Math.Round(length.TotalDays),
            current,
            previous,
            UpcomingVisits: upcoming.Count,
            UpcomingRevenue: upcoming.Sum(p => p ?? 0),
            AverageStars: reviews.Count == 0 ? null : Math.Round(reviews.Average(r => r.Stars), 1),
            ReviewCount: reviews.Count,
            NewReviews: reviews.Count(r => r.CreatedUtc >= fromUtc && r.CreatedUtc < toUtc),
            occupancy,
            Services: Rows(done, b => b.ServiceName),
            Employees: Rows(done, b => b.EmployeeName),
            weekdays,
            hours,
            weeks,
            Tips(current, setup, reviews.Count, weekdays, occupancy, openingHours.Select(o => o.DayOfWeek).ToHashSet()),
            WeeklyReportOn: !business.WeeklyReportOff,
            ProInterest: business.ProInterestUtc is not null);
    }

    public async Task<bool> SetWeeklyReportAsync(Guid businessId, bool on, CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.FindAsync([businessId], cancellationToken);
        if (business is null)
        {
            return false;
        }

        business.WeeklyReportOff = !on;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetProInterestAsync(Guid businessId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.FindAsync([businessId], cancellationToken);
        if (business is null)
        {
            return false;
        }

        business.ProInterestUtc ??= nowUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Monday 00:00 in Oslo of the week nowUtc is in
    public static DateTime WeekStartUtc(DateTime nowUtc)
    {
        var oslo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SlotCalculator.Oslo);
        var monday = DateOnly.FromDateTime(oslo).AddDays(-(((int)oslo.DayOfWeek + 6) % 7));
        return SlotCalculator.ToUtc(monday, TimeOnly.MinValue);
    }

    // A visit that took place: confirmed and over ("Møtte ikke" and cancelled ones are counted separately)
    private static bool IsDone(Visit visit, DateTime nowUtc) =>
        visit.Status == BookingStatus.Bekreftet && visit.EndUtc <= nowUtc;

    // The same person across visits: the account, otherwise the e-mail, otherwise the phone (manual bookings)
    private static string CustomerKey(string? userId, string email, string phone) =>
        userId ?? (string.IsNullOrEmpty(email) ? $"tlf:{phone}" : $"e:{email}");

    private static StatsNumbers Numbers(List<Visit> bookings, DateTime fromUtc, DateTime toUtc, DateTime nowUtc,
        Dictionary<string, DateTime> firstVisits)
    {
        var inPeriod = bookings.Where(b => b.StartUtc >= fromUtc && b.StartUtc < toUtc).ToList();
        var done = inPeriod.Where(b => IsDone(b, nowUtc)).ToList();
        var customers = done.Select(b => CustomerKey(b.CustomerUserId, b.CustomerEmail, b.CustomerPhone))
            .Distinct()
            .ToList();
        var newCustomers = customers.Count(c => firstVisits.TryGetValue(c, out var first) && first >= fromUtc);
        return new StatsNumbers(
            Visits: done.Count,
            Revenue: done.Sum(b => b.PriceNok ?? 0),
            VisitsWithoutPrice: done.Count(b => b.PriceNok is null),
            Cancelled: inPeriod.Count(b => b.Status == BookingStatus.Avlyst),
            NoShows: inPeriod.Count(b => b.Status == BookingStatus.MotteIkke),
            NewCustomers: newCustomers,
            ReturningCustomers: customers.Count - newCustomers,
            Online: done.Count(b => b.CustomerUserId is not null),
            Manual: done.Count(b => b.CustomerUserId is null));
    }

    private static List<StatsRow> Rows(List<Visit> done, Func<Visit, string> name) =>
        done.GroupBy(name)
            .Select(g => new StatsRow(g.Key, g.Count(), g.Sum(b => b.PriceNok ?? 0)))
            .OrderByDescending(r => r.Visits)
            .ThenByDescending(r => r.Revenue)
            .Take(5)
            .ToList();

    // Booked minutes out of the open minutes of all employees in the period (whole Oslo days up to today).
    // Rough: blocked time and employees who work part of the day are not known; null without opening hours
    private static int? Occupancy(List<Visit> bookings, DateTime fromUtc, DateTime toUtc, DateTime nowUtc,
        Dictionary<DayOfWeek, double> openMinutesPerDay, int employees)
    {
        var endUtc = toUtc < nowUtc ? toUtc : nowUtc;
        var firstDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(fromUtc, SlotCalculator.Oslo));
        var lastDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(endUtc, SlotCalculator.Oslo));
        double open = 0;
        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            open += openMinutesPerDay.GetValueOrDefault(day.DayOfWeek);
        }

        open *= employees;
        if (open <= 0)
        {
            return null;
        }

        var booked = bookings
            .Where(b => b.StartUtc >= fromUtc && b.StartUtc < endUtc
                                              && b.Status is BookingStatus.Bekreftet or BookingStatus.MotteIkke)
            .Sum(b => (b.EndUtc - b.StartUtc).TotalMinutes);
        return (int)Math.Min(100, Math.Round(100 * booked / open));
    }

    // Short advice from the business's own numbers, the most useful first. Pro is only mentioned for what
    // it really adds; it never moves a business up in the search
    private static List<StatsTip> Tips(StatsNumbers numbers, Setup setup, int reviews, int[] weekdays, int? occupancy,
        HashSet<DayOfWeek> openDays)
    {
        var tips = new List<StatsTip>();
        if (!setup.HasOpeningHours)
        {
            tips.Add(new StatsTip("tips", "Legg inn åpningstider",
                "Uten åpningstider kan ingen bestille på nett.", "/panel/apningstider"));
        }

        if (setup.Services == 0)
        {
            tips.Add(new StatsTip("tips", "Legg til tjenester",
                "Kundene velger en tjeneste før de ser ledige tider. Bruk forslagene for din bransje.",
                "/panel/tjenester"));
        }

        if (setup.Images < 3)
        {
            tips.Add(new StatsTip("tips", "Vis frem arbeidet ditt",
                "Legg til minst tre bilder av lokalet og arbeidet ditt, så vet kundene hva de får.",
                "/panel/galleri"));
        }

        if (!setup.HasDescription)
        {
            tips.Add(new StatsTip("tips", "Skriv litt om bedriften",
                "En kort beskrivelse på bedriftssiden gjør det lettere å velge deg.", "/panel/profil"));
        }

        if (reviews < 3)
        {
            tips.Add(new StatsTip("tips", "Få dine første vurderinger",
                "Stjernene vises fra tre vurderinger. Kundene kan vurdere deg i Min BESTI etter besøket, " +
                "så be gjerne fornøyde kunder om det.", "/panel/vurderinger"));
        }

        var planned = numbers.Visits + numbers.NoShows;
        if (planned >= MinVisitsForRates && numbers.NoShows * 10 >= planned)
        {
            tips.Add(new StatsTip("tips", "Mange møtte ikke",
                $"{numbers.NoShows} av {planned} kom ikke. Kundene får allerede påminnelse dagen før. " +
                (setup.FreeCancelHours < 24
                    ? "Med gratis avbestilling til 24 timer før får du beskjed tidligere."
                    : "En kort telefon til dem som har uteblitt, hjelper ofte."),
                setup.FreeCancelHours < 24 ? "/panel/profil" : null));
        }

        var booked = planned + numbers.Cancelled;
        if (booked >= MinVisitsForRates && numbers.Cancelled * 5 >= booked)
        {
            tips.Add(new StatsTip("tips", "Mange avbestillinger",
                $"{numbers.Cancelled} av {booked} timer ble avbestilt. Ledige tider vises med en gang for andre, " +
                "og kunder på ventelisten får beskjed.", null));
        }

        if (numbers.Visits >= MinVisitsForRates && numbers.Manual > numbers.Online)
        {
            tips.Add(new StatsTip("tips", "La kundene bestille selv",
                $"{numbers.Manual} av {numbers.Visits} timer la du inn selv. Del lenken eller QR-plakaten, " +
                "så slipper du telefonen.", "/panel/del"));
        }

        // The quietest open weekday, when it has less than half of an average open day
        var open = Enumerable.Range(0, 7).Where(i => openDays.Contains((DayOfWeek)((i + 1) % 7))).ToList();
        if (numbers.Visits >= MinVisitsForRates && open.Count >= 2)
        {
            var average = open.Average(i => weekdays[i]);
            var quietest = open.MinBy(i => weekdays[i]);
            if (weekdays[quietest] < average / 2)
            {
                tips.Add(new StatsTip("tips", $"Rolig på {WeekdayNames[quietest]}er",
                    $"Bare {weekdays[quietest]} besøk på {WeekdayNames[quietest]}er. Del en ledig tid på " +
                    "Instagram eller Facebook den dagen.", "/panel/del"));
            }
        }

        if (occupancy >= 85)
        {
            tips.Add(new StatsTip("tips", "Nesten fullbooket",
                $"Rundt {occupancy} % av tiden er booket. Lengre åpningstider eller en ansatt til gir plass til flere.",
                "/panel/apningstider"));
        }

        if (!setup.HasLoyalty && numbers.Visits >= MinVisitsForRates)
        {
            tips.Add(new StatsTip("tips", "Belønn faste kunder",
                "Med stempelkort får kunden for eksempel hvert 10. besøk gratis. Stemplene kommer av seg selv.",
                "/panel/stempelkort"));
        }

        if (!setup.AutoConfirm)
        {
            tips.Add(new StatsTip("tips", "Bekreft automatisk",
                "Kundene får svar med en gang, og du slipper å trykke Bekreft på hver time.", "/panel/profil"));
        }

        if (!setup.HasCalendarFeed)
        {
            tips.Add(new StatsTip("tips", "Timene i kalenderen på telefonen",
                "Se alle bestillinger i Google, Apple eller Outlook Kalender.", "/panel/profil"));
        }

        // Pro: at most two, and only what fits this business
        var pro = new List<StatsTip>();
        if (numbers.Visits > 0)
        {
            pro.Add(new StatsTip("pro", "Hvor kommer kundene fra?",
                "Med BESTI Pro ser du besøk på siden din og om de kom fra Instagram, Google eller QR-plakaten, " +
                "og kan laste ned tallene til Excel.", null));
        }

        if (open.Count > 0 && numbers.Visits >= MinVisitsForRates)
        {
            pro.Add(new StatsTip("pro", "Fyll de rolige timene",
                "Med BESTI Pro kan du lage tilbud for rolige dager og fremheve populære tjenester på siden din.",
                null));
        }

        if (setup.Employees >= 3)
        {
            pro.Add(new StatsTip("pro", "Flere ansatte og avdelinger",
                "BESTI Pro samler flere avdelinger og ansatte i ett panel.", null));
        }

        return [..tips.Take(5), ..pro.Take(2)];
    }

    private sealed record Visit(
        DateTime StartUtc,
        DateTime EndUtc,
        BookingStatus Status,
        decimal? PriceNok,
        string? CustomerUserId,
        string CustomerEmail,
        string CustomerPhone,
        string ServiceName,
        string EmployeeName);

    private sealed record Setup(
        bool HasOpeningHours,
        int Services,
        int Images,
        bool HasDescription,
        bool HasCalendarFeed,
        bool AutoConfirm,
        int FreeCancelHours,
        int Employees,
        bool HasLoyalty);
}
