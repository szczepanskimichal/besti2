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
    public string? CustomerNote { get; set; }
    public BookingStatus Status { get; set; }
    // "Flytt" waiting for the customer: SHA-256 of the one-time link token, and the time before the move
    public string? NewTimeTokenHash { get; set; }
    public DateTime? PreviousStartUtc { get; set; }
    // Id of the customer's account (null for bookings made before customer accounts)
    public string? CustomerUserId { get; set; }
    // When the booking was made (no reminder for a visit booked less than 12 hours before it starts)
    public DateTime CreatedUtc { get; set; }
    // When the reminder e-mail before the visit was sent; null = not yet (a new time after "Flytt" resets it)
    public DateTime? ReminderSentUtc { get; set; }
    // A visit added by the business with "Send bekreftelse på e-post" unchecked: no reminder the day before
    public bool NoReminder { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}