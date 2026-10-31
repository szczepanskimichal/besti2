using besti2.Domain.Enums;

namespace besti2.Domain.Entities;

// BESTI sammen: one line in the BESTI-chat of an invitation, or the short greeting a friend sends with
// "Jeg blir med!" / "Nei takk". Only the inviter and the friends who joined see the chat; a greeting only
// the inviter. The business never sees any of it. Removed 30 days after the visit
public class InviteMessage
{
    public Guid Id { get; set; }

    public Guid InviteId { get; set; }
    public BookingInvite Invite { get; set; } = null!;

    // null = a "Nei takk" greeting without login
    public string? UserId { get; set; }

    public InviteMessageKind Kind { get; set; }

    public string Text { get; set; } = null!;

    // A greeting picked from the ready lines carries its crystal icon (tommel, hjerte...), otherwise null
    public string? Icon { get; set; }

    // Greeting only: sent with "Jeg blir med!" (true) or with "Nei takk" (false)
    public bool Joined { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<InviteReaction> Reactions { get; set; } = [];
}
