namespace besti2.Application.Bookings;

// A visit the business adds itself (phone call, walk-in): day and time in Oslo, customer without an account
public record CreateManualBookingRequest(
    Guid ServiceId,
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly Time,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? Note,
    bool SendConfirmation
    );