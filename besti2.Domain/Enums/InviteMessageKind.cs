namespace besti2.Domain.Enums;

public enum InviteMessageKind
{
    // A line in the BESTI-chat (the inviter and the friends who joined)
    Chat,

    // "Skriv en liten hilsen": sent with the answer, only the inviter sees it
    Hilsen,
}
