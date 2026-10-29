namespace besti2.Application.Businesses;

// "Kalender på telefonen": the business's bookings as a calendar the owner subscribes to once in Google,
// Apple or Outlook Calendar. The phone then fetches it by itself, so new and moved visits show up there
public interface ICalendarFeedService
{
    // The secret part of the address, or null when it is turned off
    Task<string?> GetTokenAsync(Guid businessId, CancellationToken cancellationToken = default);

    // Turns it on, or gives a new address (the old one stops working, e.g. after a phone was lost)
    Task<string> CreateTokenAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task TurnOffAsync(Guid businessId, CancellationToken cancellationToken = default);

    // The calendar file (iCalendar) for the address; null = unknown or turned off
    Task<string?> GetCalendarAsync(string token, DateTime nowUtc, CancellationToken cancellationToken = default);
}
