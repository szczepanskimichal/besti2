namespace besti2.Application.Statistics;

public interface IBusinessStatsService
{
    // The last 7, 30, 90 or 365 days up to now, compared with the same number of days before;
    // null when the business does not exist or days is not one of the choices
    Task<BusinessStatsDto?> GetAsync(Guid businessId, int days, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // The numbers for one period (the Ukerapport uses last week, Monday to Monday in Oslo)
    Task<BusinessStatsDto?> GetForPeriodAsync(Guid businessId, DateTime fromUtc, DateTime toUtc, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // "Ukerapport på e-post" on or off
    Task<bool> SetWeeklyReportAsync(Guid businessId, bool on, CancellationToken cancellationToken = default);

    // "Jeg er interessert" in BESTI Pro (pressing it again changes nothing)
    Task<bool> SetProInterestAsync(Guid businessId, DateTime nowUtc, CancellationToken cancellationToken = default);
}

public interface IWeeklyReportService
{
    // Monday from 07:00 in Oslo: last week's numbers to every approved business that has not turned it off.
    // Returns how many were sent (each business once per week)
    Task<int> SendDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
