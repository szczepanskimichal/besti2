namespace besti2.Endpoints.Panel;

public static class PanelEndpoints
{
    // Everything under /api/panel is for a logged-in business owner only (see PanelBusiness)
    public static void MapPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/panel").RequireAuthorization(PanelBusiness.Policy);

        group.MapPanelBookingEndpoints();
        group.MapPanelBusinessEndpoints();
        group.MapPanelMediaEndpoints();
        group.MapPanelReviewEndpoints();
    }
}