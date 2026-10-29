namespace besti2.Application.Statistics;

// "Statistikk" in the business panel. Revenue is an estimate from the prices saved on the bookings:
// BESTI does not take payments, the business is paid in the salon
public record BusinessStatsDto(
    int Days,
    StatsNumbers Current,
    StatsNumbers Previous,
    int UpcomingVisits,
    decimal UpcomingRevenue,
    double? AverageStars,
    int ReviewCount,
    int NewReviews,
    int? OccupancyPercent,
    IReadOnlyList<StatsRow> Services,
    IReadOnlyList<StatsRow> Employees,
    IReadOnlyList<int> Weekdays,
    IReadOnlyList<int> Hours,
    IReadOnlyList<StatsWeek> Weeks,
    IReadOnlyList<StatsTip> Tips,
    bool WeeklyReportOn,
    bool ProInterest);

// Visits = visits that took place (Bekreftet and over). VisitsWithoutPrice = visits by agreement or per m²
// (not in Revenue). Online = booked by the customer on BESTI, Manual = added by the business
public record StatsNumbers(
    int Visits,
    decimal Revenue,
    int VisitsWithoutPrice,
    int Cancelled,
    int NoShows,
    int NewCustomers,
    int ReturningCustomers,
    int Online,
    int Manual);

public record StatsRow(string Name, int Visits, decimal Revenue);

// WeekStart = the Monday (Oslo) the week starts
public record StatsWeek(DateOnly WeekStart, int Visits, decimal Revenue);

// Kind: "tips" (something the business can do now, free) or "pro" (comes with BESTI Pro later).
// Link = the panel page where it is done
public record StatsTip(string Kind, string Title, string Text, string? Link);

public record WeeklyReportRequest(bool On);
