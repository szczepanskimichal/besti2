namespace besti2.Application.Loyalty;

public interface ILoyaltyService
{
    Task<PanelLoyaltyDto?> GetForPanelAsync(Guid businessId, DateTime nowUtc, CancellationToken cancellationToken = default);

    // Visits null = turn off. Returns null when saved, otherwise the error for the owner
    Task<string?> SetAsync(Guid businessId, SetLoyaltyRequest request, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // "Gi belønning": only when the customer has a full card; null = saved, otherwise the error
    Task<string?> RedeemAsync(Guid businessId, string customerUserId, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoyaltyCardDto>> GetMineAsync(string userId, DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
