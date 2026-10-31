using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using besti2.Application.Invites;
using besti2.Infrastructure.Identity;

namespace besti2.Endpoints;

// BESTI sammen: "Bestill med din besti". The link /invitasjon/{token} is shared on WhatsApp, Snap, Messenger
public static class InviteEndpoints
{
    public static void MapInviteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        // Send an invitation (customers only); the token comes back once, for the link
        group.MapPost("/invites", async (CreateInviteRequest request, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IInviteService inviteService, CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || user.BusinessId is not null)
            {
                return Results.BadRequest(new { message = "Logg inn med en kundekonto for å invitere." });
            }

            var result = await inviteService.CreateAsync(request, user.Id, cancellationToken);
            return result.Outcome == InviteOutcome.Created
                ? Results.Ok(new { token = result.Token, expiresUtc = result.ExpiresUtc })
                : Problem(result);
        }).RequireAuthorization().RequireRateLimiting("booking");

        // The invitation behind the link: open to everyone with the link (login only tells who is looking)
        group.MapGet("/invites/{token}", async (string token, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IInviteService inviteService, CancellationToken cancellationToken) =>
        {
            var invite = await inviteService.GetAsync(token, userManager.GetUserId(principal), cancellationToken);
            return invite is null ? NotFound() : Results.Ok(invite);
        }).RequireRateLimiting("search");

        // "Jeg blir med!": both visits are booked now (customers only; name and e-mail come from the account)
        group.MapPost("/invites/{token}/accept", async (string token, AcceptInviteRequest request,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IInviteService inviteService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || user.BusinessId is not null)
            {
                return Results.BadRequest(new { message = "Logg inn med en kundekonto for å bli med." });
            }

            var result = await inviteService.AcceptAsync(token, user.Id, request, cancellationToken);
            return result.Outcome == InviteOutcome.Accepted ? Results.NoContent() : Problem(result);
        }).RequireAuthorization().RequireRateLimiting("booking");

        // "Nei takk": no login needed (a greeting to the inviter is optional)
        group.MapPost("/invites/{token}/decline", async (string token, DeclineInviteRequest? request,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IInviteService inviteService,
            CancellationToken cancellationToken) =>
        {
            var result = await inviteService.DeclineAsync(token, userManager.GetUserId(principal), request,
                cancellationToken);
            return result.Outcome == InviteOutcome.Declined ? Results.NoContent() : Problem(result);
        }).RequireRateLimiting("booking");

        // BESTI-chat: only the inviter and the friends who joined (the business never sees it)
        group.MapPost("/invites/{token}/messages", async (string token, SendInviteMessageRequest request,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IInviteService inviteService,
            CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Chat(await inviteService.SendMessageAsync(token, userId, request, cancellationToken));
        }).RequireAuthorization().RequireRateLimiting("chat");

        group.MapDelete("/invites/{token}/messages/{id:guid}", async (string token, Guid id,
            ClaimsPrincipal principal, UserManager<AppUser> userManager, IInviteService inviteService,
            CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Chat(await inviteService.DeleteMessageAsync(token, userId, id, cancellationToken));
        }).RequireAuthorization().RequireRateLimiting("chat");

        // A crystal reaction: the same kind again takes it back
        group.MapPost("/invites/{token}/messages/{id:guid}/reactions", async (string token, Guid id,
            InviteReactionRequest request, ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IInviteService inviteService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Chat(await inviteService.ToggleReactionAsync(token, userId, id, request, cancellationToken));
        }).RequireAuthorization().RequireRateLimiting("chat");

        // Min Besti: invitations I sent or answered
        group.MapGet("/me/invites", async (ClaimsPrincipal principal, UserManager<AppUser> userManager,
            IInviteService inviteService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            return userId is null
                ? Results.Unauthorized()
                : Results.Ok(await inviteService.GetForCustomerAsync(userId, cancellationToken));
        }).RequireAuthorization();

        // Take a waiting invitation back
        group.MapPost("/me/invites/{id:guid}/trekk", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IInviteService inviteService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await inviteService.WithdrawAsync(id, userId, cancellationToken);
            return result.Outcome == InviteOutcome.Declined ? Results.NoContent() : Problem(result);
        }).RequireAuthorization();

        // "Del igjen": a new link for a waiting invitation (the old one stops working)
        group.MapPost("/me/invites/{id:guid}/lenke", async (Guid id, ClaimsPrincipal principal,
            UserManager<AppUser> userManager, IInviteService inviteService, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await inviteService.NewLinkAsync(id, userId, cancellationToken);
            return result.Outcome == InviteOutcome.Created
                ? Results.Ok(new { token = result.Token, expiresUtc = result.ExpiresUtc })
                : Problem(result);
        }).RequireAuthorization().RequireRateLimiting("booking");
    }

    private static IResult Chat(InviteChatOutcome outcome) => outcome switch
    {
        InviteChatOutcome.Done => Results.NoContent(),
        InviteChatOutcome.Invalid => Results.BadRequest(
            new { message = $"Skriv en melding på maks {InviteChatRules.MaxMessageLength} tegn." }),
        InviteChatOutcome.NotAllowed => Results.Json(
            new { message = "Bare de som er med i gruppa kan skrive her." }, statusCode: StatusCodes.Status403Forbidden),
        _ => NotFound()
    };

    private static IResult NotFound() =>
        Results.NotFound(new { message = "Invitasjonen finnes ikke. Kanskje lenken er gammel?" });

    // Every "no" as a short Norwegian sentence for the web page
    private static IResult Problem(InviteResult result) => result.Outcome switch
    {
        InviteOutcome.Invalid => Results.BadRequest(new { message = result.Error }),
        InviteOutcome.Expired => Results.Conflict(
            new { message = "Svarfristen er ute. Ingenting ble bestilt.", outcome = "Expired" }),
        InviteOutcome.AlreadyAnswered => Results.Conflict(
            new { message = "Invitasjonen er allerede besvart.", outcome = "AlreadyAnswered" }),
        InviteOutcome.OwnInvite => Results.BadRequest(
            new { message = "Dette er din egen invitasjon. Del lenken med vennen din.", outcome = "OwnInvite" }),
        InviteOutcome.SlotTaken => Results.Conflict(
            new { message = "Tiden er dessverre ikke lenger ledig for gruppa.", outcome = "SlotTaken" }),
        InviteOutcome.CustomerBusy => Results.Conflict(
            new { message = "Du har allerede en time på dette tidspunktet.", outcome = "CustomerBusy" }),
        InviteOutcome.AlreadyJoined => Results.Conflict(
            new { message = "Du er allerede med i gruppa. Timen din ligger i Min BESTI.", outcome = "AlreadyJoined" }),
        InviteOutcome.Full => Results.Conflict(
            new { message = "Gruppa er dessverre full.", outcome = "Full" }),
        _ => NotFound()
    };
}
