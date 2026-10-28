namespace besti2.Application.Invites;

// BESTI sammen: invite friends or a team (2–8 visits) to the same service at the same time with one link.
// Nothing is booked before someone says yes; each yes books that person at once
public interface IInviteService
{
    Task<InviteResult> CreateAsync(CreateInviteRequest request, string inviterUserId,
        CancellationToken cancellationToken = default);

    // Public preview behind the link; viewerUserId = the logged-in visitor, if any (null = wrong link)
    Task<InviteDto?> GetAsync(string token, string? viewerUserId, CancellationToken cancellationToken = default);

    // "Jeg blir med!": checks the time again and books the visitor (and the inviter, on the first yes)
    Task<InviteResult> AcceptAsync(string token, string friendUserId, AcceptInviteRequest request,
        CancellationToken cancellationToken = default);

    // "Nei takk" works without login: the link itself is the proof (in a group it only tells the inviter)
    Task<InviteResult> DeclineAsync(string token, string? userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerInviteDto>> GetForCustomerAsync(string userId,
        CancellationToken cancellationToken = default);

    // The inviter takes back an invitation nobody has said yes to yet
    Task<InviteResult> WithdrawAsync(Guid inviteId, string inviterUserId,
        CancellationToken cancellationToken = default);

    // "Del igjen" from Min Besti: a new link for a waiting invitation (the old link stops working)
    Task<InviteResult> NewLinkAsync(Guid inviteId, string inviterUserId,
        CancellationToken cancellationToken = default);

    // Every 15 minutes: closes groups whose answer time is up; the business gets the final count
    Task<int> CloseDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
