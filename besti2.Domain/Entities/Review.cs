namespace besti2.Domain.Entities;

// One review per booking, written by the customer after the visit
public class Review
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid BusinessId { get; set; }
    public string CustomerUserId { get; set; } = null!;
    public string CustomerName { get; set; } = null!; // first name only, shown publicly
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public string? BusinessReply { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? RepliedUtc { get; set; }

    // An optional photo from the visit (file name in the image storage, see IImageStorage)
    public string? PhotoFileName { get; set; }

    // Hidden by BESTI (e.g. hateful words or personal details): not shown and not counted in the stars
    public DateTime? HiddenUtc { get; set; }
    public string? HiddenReason { get; set; }
}