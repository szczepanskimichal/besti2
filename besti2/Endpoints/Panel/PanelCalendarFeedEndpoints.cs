using besti2.Application.Businesses;

namespace besti2.Endpoints.Panel;

// "Kalender på telefonen" in the profile: turn on, get a new address, turn off
public static class PanelCalendarFeedEndpoints
{
    public static void MapPanelCalendarFeedEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/calendar-feed", async (PanelBusiness business, ICalendarFeedService calendarFeed,
                CancellationToken cancellationToken) =>
            Results.Ok(new { token = await calendarFeed.GetTokenAsync(business.Id, cancellationToken) }));

        group.MapPost("/calendar-feed", async (PanelBusiness business, ICalendarFeedService calendarFeed,
                CancellationToken cancellationToken) =>
            Results.Ok(new { token = await calendarFeed.CreateTokenAsync(business.Id, cancellationToken) }));

        group.MapDelete("/calendar-feed", async (PanelBusiness business, ICalendarFeedService calendarFeed,
            CancellationToken cancellationToken) =>
        {
            await calendarFeed.TurnOffAsync(business.Id, cancellationToken);
            return Results.NoContent();
        });
    }
}
