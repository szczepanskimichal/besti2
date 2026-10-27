namespace besti2.Application.Bookings;

// EmployeeId null = the whole business
public record TimeOffDto(
    Guid Id,
    Guid? EmployeeId,
    string? EmployeeName,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Reason);

// Days and times in Oslo; AllDay = from the start of FromDate to the end of ToDate (times are ignored)
public record CreateTimeOffRequest(
    Guid? EmployeeId,
    DateOnly FromDate,
    TimeOnly FromTime,
    DateOnly ToDate,
    TimeOnly ToTime,
    bool AllDay,
    string? Reason);

// Error = the message for the form; OverlappingBookings = visits already in that time (they are not cancelled)
public record CreateTimeOffResult(TimeOffDto? TimeOff, int OverlappingBookings = 0, string? Error = null);