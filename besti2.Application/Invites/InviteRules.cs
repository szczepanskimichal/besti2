namespace besti2.Application.Invites;

// BESTI sammen rules (2 people decided 2026-10-23, groups 2026-10-28)
public static class InviteRules
{
    // From two besties up to a whole team (one employee per person, all at the same time)
    public const int MinPersons = 2;
    public const int MaxPersons = 8;

    // The inviter picks how long the others have to answer: short for two (an impulse), up to a day for a group
    public static readonly int[] DecisionMinutes = [15, 30, 60, 180, 1440];

    public const int DefaultDecisionMinutes = 30;
}
