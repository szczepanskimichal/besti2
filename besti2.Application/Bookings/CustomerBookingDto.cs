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
    string BusinessAddress,
    Guid ServiceId,
    Guid EmployeeId,
    bool IsReviewed,
    DateTime? PreviousStartUtc = null // set while a moved visit waits for the customer's answer
);