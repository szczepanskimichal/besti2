using besti2.Domain.Enums;

namespace besti2.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public bool MarketingConsent { get; set; }
    public DateTime? MarketingConsentUtc { get; set; }
    public BookingStatus Status { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}