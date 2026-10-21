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
    
    public List<Employee> Employees { get; set; } = new();
    public List<Service> Services { get; set; } = new();
    public List<OpeningHours> OpeningHours { get; set; } = new();
    public List<BusinessImage> Images { get; set; } = new();
}