using besti2.Domain.Enums;


namespace besti2.Domain.Entities;

public class Business
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string OrgNumber { get; set; } = null!;
    public BusinessCategory Category { get; set; }

    // Public contact e-mail shown to customers (optional). The owner logs in with another, private address
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? Description { get; set; }

    // The business's own website (optional), always starting with http:// or https://
    public string? Website { get; set; }

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

    // Free cancellation until this many hours before a confirmed visit; 0 = any time before it starts
    public int FreeCancelHours { get; set; } = 24;

    // The only choices the business can pick in the panel
    public static readonly int[] FreeCancelOptions = [0, 2, 12, 24, 48];

    // New online bookings are confirmed at once (false = the business presses "Bekreft" on each one)
    public bool AutoConfirm { get; set; }

    public List<Employee> Employees { get; set; } = new();
    public List<Service> Services { get; set; } = new();
    public List<OpeningHours> OpeningHours { get; set; } = new();
    public List<BusinessImage> Images { get; set; } = new();
}