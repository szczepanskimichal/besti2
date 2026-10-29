namespace besti2.Application.Bookings;

public interface IBookingExportService
{
    // "Last ned" in Statistikk: every booking from..to (days in Oslo) as a CSV file for Excel and the accountant;
    // null = the period is not allowed
    Task<string?> BookingsCsvAsync(Guid businessId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);
}
