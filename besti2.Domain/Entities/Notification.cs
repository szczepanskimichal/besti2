namespace besti2.Domain.Entities;

// A message in the bell (🔔) for one customer (UserId) or one business (BusinessId)
public class Notification
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }

    public Guid? BusinessId { get; set; }

    // The visit it is about (null for e.g. a review); used to clear it once the customer has answered
    public Guid? BookingId { get; set; }

    public string Text { get; set; } = null!;

    // Where a click leads, e.g. "/min-besti" or "/panel?dag=2026-10-22&bestilling=..."
    public string? Link { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ReadUtc { get; set; }

    // When it went out as a push message to the phone (or was skipped); null = not looked at yet
    public DateTime? PushedUtc { get; set; }
}