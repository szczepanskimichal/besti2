namespace besti2.Domain.Entities;

// "Si fra hvis det blir ledig": a customer waits for a time with this service on this day. When another
// customer cancels (or the business opens more time), the customer gets a message in the bell and on the phone
public class WaitlistEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    // null = Hvem som helst
    public Guid? EmployeeId { get; set; }

    // The day in Oslo
    public DateOnly Date { get; set; }
    public DateTime CreatedUtc { get; set; }

    // The free times we already know about ("0900,1430"): only a NEW free time is news for the customer
    public string KnownFreeTimes { get; set; } = "";

    // When the customer got the message; null = still waiting (one message, then the entry is done)
    public DateTime? NotifiedUtc { get; set; }
}
