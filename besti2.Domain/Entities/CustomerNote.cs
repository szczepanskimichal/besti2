namespace besti2.Domain.Entities;

// "Kunder" in the panel: the business's own note about one customer ("liker det kort", "allergisk mot farge").
// A customer with an account is found by CustomerUserId, a visit added by phone by CustomerPhone
public class CustomerNote
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public string? CustomerUserId { get; set; }
    public string? CustomerPhone { get; set; }
    public string Text { get; set; } = null!;
    public DateTime UpdatedUtc { get; set; }
}
