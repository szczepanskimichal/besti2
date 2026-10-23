namespace besti2.Application.Businesses;

// What BESTI sees on the approval page before clicking "Godkjenn"
public record BusinessApprovalDto(
    string Name,
    string OrgNumber,
    string Category,
    string Address,
    string PostalCode,
    string City,
    string Email,
    string PhoneNumber);

public record ApproveBusinessRequest(string Token);