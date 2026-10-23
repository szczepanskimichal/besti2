namespace besti2.Application.Businesses;

public record RegisterBusinessRequest(
    string BusinessName,
    string OrgNumber,
    string Category,
    string Email,
    string Phone,
    string Address,
    string PostalCode,
    string City,
    string Password,
    bool AcceptDataProcessing = false
);