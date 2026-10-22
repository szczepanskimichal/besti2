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
    DateTime? PreviousStartUtc = null // set while a moved visit waits for the customer
    );