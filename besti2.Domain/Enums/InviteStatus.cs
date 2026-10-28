namespace besti2.Domain.Enums;

public enum InviteStatus
{
    // Sent, nobody has said yes yet (nothing is booked before "Jeg blir med!")
    Venter,

    // At least one said yes and is booked; more may join while places are left and time is not up
    Akseptert,

    // The only friend said "Nei takk" (in a group "Nei takk" only tells the inviter)
    Avslatt,

    // Nobody answered in time
    Utlopt,

    // The inviter took the invitation back
    Trukket,
}
