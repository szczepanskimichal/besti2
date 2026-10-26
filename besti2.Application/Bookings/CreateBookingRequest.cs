namespace besti2.Application.Bookings;

public record CreateBookingRequest(
    Guid ServiceId,
    Guid EmployeeId,
    DateTime StartUtc,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    bool MarketingConsent,
    string? Note=null,
    int Units = 1 // "Per time" services: how many hours in a row
    );