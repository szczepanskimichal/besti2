namespace besti2.Application.Invites;

// BESTI-chat in an invitation (decided 2026-10-31): only the inviter and the friends who joined can read and
// write; a greeting with the answer goes only to the inviter; the business never sees it
public static class InviteChatRules
{
    public const int MaxMessageLength = 500;
    public const int MaxGreetingLength = 200;

    // The crystal reactions (web/public/ikoner/{kind}.svg); a greeting icon is one of them too
    public static readonly string[] Reactions = ["hjerte", "tommel", "latter", "trist", "stjerne", "feiring"];

    // A private talk between friends: gone 30 days after the visit
    public const int KeepDaysAfterVisit = 30;

    // The others get at most one bell per invitation in this time, however many lines are written
    public const int BellQuietMinutes = 10;
}
