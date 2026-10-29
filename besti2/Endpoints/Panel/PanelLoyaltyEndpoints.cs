using besti2.Application.Loyalty;

namespace besti2.Endpoints.Panel;

// "Stempelkort": the business turns the card on, sees who has stamps and gives the reward
public static class PanelLoyaltyEndpoints
{
    public static void MapPanelLoyaltyEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/loyalty", async (PanelBusiness business, ILoyaltyService loyaltyService,
            CancellationToken cancellationToken) =>
        {
            var card = await loyaltyService.GetForPanelAsync(business.Id, DateTime.UtcNow, cancellationToken);
            return card is null ? Results.NotFound() : Results.Ok(card);
        });

        group.MapPut("/loyalty", async (SetLoyaltyRequest request, PanelBusiness business,
            ILoyaltyService loyaltyService, CancellationToken cancellationToken) =>
        {
            var error = await loyaltyService.SetAsync(business.Id, request, DateTime.UtcNow, cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });

        group.MapPost("/loyalty/redeem", async (RedeemLoyaltyRequest request, PanelBusiness business,
            ILoyaltyService loyaltyService, CancellationToken cancellationToken) =>
        {
            var error = await loyaltyService.RedeemAsync(business.Id, request.CustomerUserId, DateTime.UtcNow,
                cancellationToken);
            return error is null ? Results.NoContent() : Results.BadRequest(new { message = error });
        });
    }
}
