using besti2.Domain.Enums;


namespace besti2.Domain.Entities;

public class Business
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string OrgNumber { get; set; } = null!;
    public BusinessCategory Category { get; set; }

    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? Description { get; set; }

    // Map position (WGS84), filled from the address; null when not found
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // A new business is hidden from customers until the owner has confirmed the email
    public bool IsHidden { get; set; }
    // Set when BESTI approves the business; until then it stays hidden even with a confirmed e-mail
    public DateTime? ApprovedUtc { get; set; }
    // The one-click approval link sent to BESTI (only the hash is stored)
    public string? ApprovalTokenHash { get; set; }
    // When the owner accepted the data processing agreement (databehandleravtale) at sign-up
    public DateTime? DataProcessingAcceptedUtc { get; set; }
    
    public List<Employee> Employees { get; set; } = new();
    public List<Service> Services { get; set; } = new();
    public List<OpeningHours> OpeningHours { get; set; } = new();
    public List<BusinessImage> Images { get; set; } = new();
}