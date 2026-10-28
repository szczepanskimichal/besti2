namespace besti2.Application.Invites;

// "Bestill med din besti": the inviter picks the time and how many visits in total (2–8).
// OrganizerJoins = false: the inviter only organizes (a boss sending the team), then all places are for the others.
// PayMode = "HverSelv" or "JegSpanderer"; DecisionMinutes = 15, 30, 60, 180 or 1440
public record CreateInviteRequest(
    Guid ServiceId,
    Guid? EmployeeId,
    DateTime StartUtc,
    string Phone,
    string PayMode,
    int DecisionMinutes = InviteRules.DefaultDecisionMinutes,
    string? Note = null,
    int Units = 1,
    int Persons = 2,
    bool OrganizerJoins = true);

public enum InviteOutcome
{
    Created,
    Accepted,
    Declined,
    Invalid,
    NotFound,
    Expired,
    AlreadyAnswered,
    AlreadyJoined,
    Full,
    OwnInvite,
    SlotTaken,
    CustomerBusy
}

// Token only right after "Send": it is the secret part of the link /invitasjon/{token} (only its hash is stored)
public record InviteResult(InviteOutcome Outcome, string? Token = null, DateTime? ExpiresUtc = null,
    string? Error = null);

// "Jeg blir med!": phone for the visitor's own booking (name and e-mail come from the account)
public record AcceptInviteRequest(string Phone, string? Note = null);

// What everyone with the link sees on /invitasjon/{token}: first names only, never contact details.
// Persons = all visits; Joined = booked so far (the inviter's own included); PlacesLeft = places still free
// for others; Open = new people can still join; IsMember = the visitor is booked in it;
// StillFree = the visitor's place (and the inviter's, before the first yes) is still free right now
public record InviteDto(
    Guid Id,
    string Status,
    string InviterFirstName,
    IReadOnlyList<string> Names,
    string BusinessName,
    string BusinessSlug,
    string BusinessAddress,
    string ServiceName,
    int VisitMinutes,
    decimal? PriceNok,
    string PriceType,
    DateTime StartUtc,
    DateTime ExpiresUtc,
    string PayMode,
    int Persons,
    bool OrganizerJoins,
    int Joined,
    int PlacesLeft,
    bool Open,
    bool IsInviter,
    bool IsMember,
    bool StillFree);

// One invitation in Min Besti (sent by me, or one I joined)
public record CustomerInviteDto(
    Guid Id,
    string Status,
    bool IsInviter,
    string InviterFirstName,
    IReadOnlyList<string> Names, // first names of everyone booked, the inviter first
    string BusinessName,
    string BusinessSlug,
    string ServiceName,
    DateTime StartUtc,
    DateTime ExpiresUtc,
    string PayMode,
    int Persons,
    int Joined,
    bool Open);
