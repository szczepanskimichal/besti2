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

    // Failed reminder e-mails in a row; after BookingReminder.MaxAttempts it stops trying
    public int ReminderAttempts { get; set; }

    // When the "Møtte ikke" e-mail was sent; it goes only once, even if the business switches back and forth
    public DateTime? NoShowEmailSentUtc { get; set; }

    // A visit added by the business with "Send bekreftelse på e-post" unchecked: no reminder the day before
    public bool NoReminder { get; set; }

    // When the "Om 2 timer" notice went to the customer's bell and phone (only once)
    public DateTime? SoonNoticeSentUtc { get; set; }

    // When the business was reminded that this booking still waits for "Bekreft" (only once)
    public DateTime? PendingReminderSentUtc { get; set; }

    // BESTI sammen: both visits booked from the same invitation share this id (null = a normal booking)
    public Guid? InviteId { get; set; }

    // The price when the visit was booked (fixed price, or the hourly price times the hours); null = by agreement,
    // per m² and similar. Kept here because the business may change the service price later (Statistikk)
    public decimal? PriceNok { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}