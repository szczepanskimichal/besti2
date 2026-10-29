namespace besti2.Application.Loyalty;

// "Stempelkort" in the panel: the settings and the customers who have stamps now
public record PanelLoyaltyDto(int? Visits, string? Reward, DateTime? StartUtc, IReadOnlyList<LoyaltyCustomerDto> Customers);

// Stamps = on the current round (0..Visits); RewardReady = the business can give the reward now
public record LoyaltyCustomerDto(string CustomerUserId, string Name, int Stamps, bool RewardReady,
    DateTime LastVisitUtc, int RewardsGiven);

public record SetLoyaltyRequest(int? Visits, string? Reward);

public record RedeemLoyaltyRequest(string CustomerUserId);

// The customer's cards in Min Besti (only businesses with a stamp card where the customer has visited)
public record LoyaltyCardDto(Guid BusinessId, string BusinessName, string BusinessSlug, int Visits, string Reward,
    int Stamps, bool RewardReady, int RewardsGiven);
