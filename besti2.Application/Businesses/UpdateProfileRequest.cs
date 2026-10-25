namespace besti2.Application.Businesses;

public record UpdateProfileRequest(
    string? Description,
    string Phone,
    string Address,
    string PostalCode,
    string City,
    // Public e-mail for customers; empty removes it
    string? ContactEmail = null
);