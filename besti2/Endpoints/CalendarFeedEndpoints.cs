using besti2.Application.Businesses;

namespace besti2.Endpoints;

public static class CalendarFeedEndpoints
{
    // "Kalender på telefonen": Google, Apple or Outlook Calendar fetches this by itself every so often.
    // No login (a calendar app cannot log in): the long secret address is the key, and the owner can change it
    public static void MapCalendarFeedEndpoints(this WebApplication app)
    {
        app.MapGet("/api/calendar/{token}", async (string token, ICalendarFeedService calendarFeed,
            CancellationToken cancellationToken) =>
        {
            var ics = await calendarFeed.GetCalendarAsync(token, DateTime.UtcNow, cancellationToken);
            return ics is null ? Results.NotFound() : Results.Text(ics, "text/calendar; charset=utf-8");
        });
    }
}
