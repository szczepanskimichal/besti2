namespace besti2.Domain.Entities;

// "Stempelkort": the business gave a customer the reward; the card then starts on a new round
public class LoyaltyRedemption
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public string CustomerUserId { get; set; } = null!;
    public DateTime RedeemedUtc { get; set; }
}
