namespace besti2.Domain.Entities;

// "Varsler på telefonen": one phone or computer that gets the bell's notifications as a push message, even
// with BESTI closed. Endpoint and keys come from the browser (Web Push); one person can have several devices
public class WebPushSubscription
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;

    // Set for a business owner: the business's notifications go to this device
    public Guid? BusinessId { get; set; }

    // The push service's address for this device (Google, Apple, Mozilla or Microsoft)
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
}
