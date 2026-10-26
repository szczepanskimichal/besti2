namespace besti2.Application.Businesses;

public record UpdateProfileRequest(
    string? Description,
    string Phone,
    string Address,
    string PostalCode,
    string City,
    // Public e-mail for customers; empty removes it
    string? ContactEmail = null,
    // Free cancellation until X hours before the visit (0, 2, 12, 24 or 48)
    int FreeCancelHours = 24
    );