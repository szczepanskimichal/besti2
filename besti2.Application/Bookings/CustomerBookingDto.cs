namespace besti2.Application.Bookings;

// One booking as the customer sees it on "Mine bestillinger"
public record CustomerBookingDto(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string ServiceName,
    string EmployeeName,
    Guid BusinessId,
    string BusinessName,
    string BusinessSlug, // link to the business page: /{BusinessSlug}
    string BusinessAddress,
    Guid ServiceId,
    Guid EmployeeId,
    bool IsReviewed,
    string BusinessPhone, // after the free-cancel deadline the customer calls the business
    int FreeCancelHours,
    DateTime? PreviousStartUtc = null, // set while a moved visit waits for the customer's answer
    Guid? InviteId = null // BESTI sammen: booked together with a friend
);