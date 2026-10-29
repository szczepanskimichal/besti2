namespace besti2.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;

    // Soft delete: old bookings still point to this employee
    public bool IsDeleted { get; set; }

    // "Egen innlogging": the employee sees their own calendar under /ansatt with their own BESTI account.
    // The owner invites an e-mail; the link in the e-mail (only its hash is stored) connects the account
    public string? StaffEmail { get; set; }
    public string? StaffInviteTokenHash { get; set; }
    public DateTime? StaffInviteExpiresUtc { get; set; }

    // The connected account (null = no access yet)
    public string? StaffUserId { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
}