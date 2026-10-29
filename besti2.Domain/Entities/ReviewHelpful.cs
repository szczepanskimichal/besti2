namespace besti2.Domain.Entities;

// "Nyttig": a logged-in customer found a review helpful (one per review and person)
public class ReviewHelpful
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
}
