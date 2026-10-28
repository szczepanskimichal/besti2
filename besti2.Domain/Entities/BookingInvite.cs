using besti2.Domain.Enums;

namespace besti2.Domain.Entities;

// BESTI sammen: "Bestill med din besti". One customer invites friends (or a whole team) to the same service
// at the same time, with one shared link. Nothing is booked before someone says yes; each "Jeg blir med!"
// books that person's visit at once, the inviter's own visit together with the first yes (Booking.InviteId)
public class BookingInvite
{
    public Guid Id { get; set; }

    // SHA-256 of the link token (/invitasjon/{token}); the token itself is only in the shared link
    public string TokenHash { get; set; } = null!;

    public string InviterUserId { get; set; } = null!;

    // The inviter's phone for their own booking (name and e-mail come from the account)
    public string InviterPhone { get; set; } = null!;

    public string? InviterNote { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    // The employee the inviter picked for themselves (null = Hvem som helst); the friend gets another one
    public Guid? EmployeeId { get; set; }

    public DateTime StartUtc { get; set; }

    // "Per time" services: how many hours in a row (same for both)
    public int Units { get; set; } = 1;

    // How many visits in total (the inviter's own included when OrganizerJoins), 2 to 8
    public int Persons { get; set; } = 2;

    // false = the inviter only organizes (and maybe pays), e.g. a boss sending the team to the spa
    public bool OrganizerJoins { get; set; } = true;

    public PayMode PayMode { get; set; }
    public InviteStatus Status { get; set; }

    // Until when the friend can say yes (the inviter picks 15, 30 or 60 minutes)
    public DateTime ExpiresUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    // The first yes (or the only friend's "Nei takk", or "Trekk tilbake")
    public DateTime? AnsweredUtc { get; set; }

    // When the group was closed (full or time up) and the business got the final count; null = still open
    public DateTime? ClosedUtc { get; set; }
}
