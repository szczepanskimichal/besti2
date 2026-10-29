namespace besti2.Application.Bookings;

public record PanelBookingDto(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string ServiceName,
    string EmployeeName,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string? CustomerNote,
    Guid EmployeeId, // the calendar puts the booking in this employee's column
    DateTime? PreviousStartUtc = null, // set while a moved visit waits for the customer
    int NoShowCount = 0, // "Møtte ikke" for this customer at this business
    Guid? InviteId = null, // BESTI sammen: all visits of one group share it (👯 in the calendar)
    PanelGroupDto? Group = null,
    string? CustomerKey = null, // "Kunder": the customer's card (null = anonymized)
    string? SalonNote = null // the business's own note about this customer
);

// BESTI sammen seen by the business: how many places, how many said yes, until when the others can answer.
// Open = more people may still join; when it closes, the count is final
public record PanelGroupDto(
    Guid Id,
    int Persons,
    int Joined,
    DateTime ExpiresUtc,
    bool Open,
    string OrganizerName,
    bool OrganizerComes,
    bool OrganizerPays, // "Jeg spanderer": the organizer pays for the whole group
    bool IsOrganizer, // this booking is the organizer's own visit
    IReadOnlyList<string> Names // everyone booked, in the order they said yes
);