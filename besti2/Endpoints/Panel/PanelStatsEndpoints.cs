using besti2.Application.Statistics;

namespace besti2.Endpoints.Panel;

// "Statistikk": visits, estimated revenue and tips, the Ukerapport switch and interest in BESTI Pro
public static class PanelStatsEndpoints
{
    public static void MapPanelStatsEndpoints(this RouteGroupBuilder group)
    {
        // days = 7, 30, 90 or 365 (the last days up to now)
        group.MapGet("/stats", async (int? days, PanelBusiness business, IBusinessStatsService statsService,
            CancellationToken cancellationToken) =>
        {
            var stats = await statsService.GetAsync(business.Id, days ?? 30, DateTime.UtcNow, cancellationToken);
            return stats is null ? Results.BadRequest(new { message = "Velg en periode." }) : Results.Ok(stats);
        });

        group.MapPut("/weekly-report", async (WeeklyReportRequest request, PanelBusiness business,
                IBusinessStatsService statsService, CancellationToken cancellationToken) =>
            await statsService.SetWeeklyReportAsync(business.Id, request.On, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        group.MapPost("/pro-interest", async (PanelBusiness business, IBusinessStatsService statsService,
                CancellationToken cancellationToken) =>
            await statsService.SetProInterestAsync(business.Id, DateTime.UtcNow, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());
    }
}
