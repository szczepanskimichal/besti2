namespace besti2.Domain.Entities;

// A crystal reaction on a BESTI-chat line: one of each kind per person, a second tap takes it back
public class InviteReaction
{
    public Guid MessageId { get; set; }
    public InviteMessage Message { get; set; } = null!;

    public string UserId { get; set; } = null!;

    // hjerte, tommel, latter, trist, stjerne or feiring (InviteChatRules.Reactions)
    public string Kind { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }
}
