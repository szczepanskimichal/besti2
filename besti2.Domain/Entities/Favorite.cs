namespace besti2.Domain.Entities;

// "Favoritter": a business the customer saved with the heart on the business page
public class Favorite
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
}
