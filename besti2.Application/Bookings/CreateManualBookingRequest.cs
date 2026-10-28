namespace besti2.Application.Bookings;

// A visit the business adds itself (phone call, walk-in): day and time in Oslo, customer without an account.
// Units: "Per time" services may take several times the service length in a row (max 8 hours), like online
public record CreateManualBookingRequest(
    Guid ServiceId,
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly Time,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? Note,
    bool SendConfirmation,
    int Units = 1
);