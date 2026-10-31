using besti2.Application.Invites;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services.Invites;

// BESTI-chat inside an invitation (decided 2026-10-31): the inviter and the friends who joined talk here
// (who brings what, where to meet) instead of starting yet another group elsewhere. A greeting sent with the
// answer goes only to the inviter. The business never sees any of it; it all goes 30 days after the visit
public partial class InviteService
{
    // The viewer's own name in reactions, as the web page shows it
    private const string Me = "Du";

    public async Task<InviteChatOutcome> SendMessageAsync(string token, string userId,
        SendInviteMessageRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.Text?.Trim() ?? "";
        if (text.Length == 0 || text.Length > InviteChatRules.MaxMessageLength)
        {
            return InviteChatOutcome.Invalid;
        }

        var (invite, members) = await ChatAccessAsync(token, userId, cancellationToken);
        if (invite is null)
        {
            return members is null ? InviteChatOutcome.NotFound : InviteChatOutcome.NotAllowed;
        }

        var nowUtc = DateTime.UtcNow;
        dbContext.InviteMessages.Add(new InviteMessage
        {
            Id = Guid.NewGuid(),
            InviteId = invite.Id,
            UserId = userId,
            Kind = InviteMessageKind.Chat,
            Text = text,
            CreatedUtc = nowUtc
        });

        // The others get a bell, but at most one per invitation every 10 minutes (a lively chat is not spam)
        var link = ChatLink(invite.Id);
        var quietSince = nowUtc.AddMinutes(-InviteChatRules.BellQuietMinutes);
        var recipients = members!.Append(invite.InviterUserId).Where(id => id != userId).Distinct().ToList();
        var recentlyRung = await dbContext.Notifications
            .Where(n => n.UserId != null && recipients.Contains(n.UserId) && n.Link == link
                        && n.CreatedUtc > quietSince)
            .Select(n => n.UserId!)
            .ToListAsync(cancellationToken);
        var author = (await FirstNamesAsync([userId], cancellationToken)).GetValueOrDefault(userId, "En venn");
        var preview = text.Length <= 60 ? text : text[..59] + "…";
        foreach (var recipient in recipients.Except(recentlyRung))
        {
            AddNotification(recipient, null, $"💬 {author} i BESTI-chatten: «{preview}»", link, null);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return InviteChatOutcome.Done;
    }

    public async Task<InviteChatOutcome> ToggleReactionAsync(string token, string userId, Guid messageId,
        InviteReactionRequest request, CancellationToken cancellationToken = default)
    {
        if (!InviteChatRules.Reactions.Contains(request.Kind))
        {
            return InviteChatOutcome.Invalid;
        }

        var (invite, members) = await ChatAccessAsync(token, userId, cancellationToken);
        if (invite is null)
        {
            return members is null ? InviteChatOutcome.NotFound : InviteChatOutcome.NotAllowed;
        }

        var exists = await dbContext.InviteMessages.AnyAsync(m => m.Id == messageId && m.InviteId == invite.Id
                                                                  && m.Kind == InviteMessageKind.Chat,
            cancellationToken);
        if (!exists)
        {
            return InviteChatOutcome.NotFound;
        }

        // A second tap on the same crystal takes it back
        var reaction = await dbContext.InviteReactions.FirstOrDefaultAsync(
            r => r.MessageId == messageId && r.UserId == userId && r.Kind == request.Kind, cancellationToken);
        if (reaction is null)
        {
            dbContext.InviteReactions.Add(new InviteReaction
            {
                MessageId = messageId, UserId = userId, Kind = request.Kind, CreatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.InviteReactions.Remove(reaction);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // A double tap: the first one already added it
        }

        return InviteChatOutcome.Done;
    }

    public async Task<InviteChatOutcome> DeleteMessageAsync(string token, string userId, Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var (invite, members) = await ChatAccessAsync(token, userId, cancellationToken);
        if (invite is null)
        {
            return members is null ? InviteChatOutcome.NotFound : InviteChatOutcome.NotAllowed;
        }

        var message = await dbContext.InviteMessages.FirstOrDefaultAsync(
            m => m.Id == messageId && m.InviteId == invite.Id && m.Kind == InviteMessageKind.Chat,
            cancellationToken);
        if (message is null)
        {
            return InviteChatOutcome.NotFound;
        }

        // Your own lines, and the inviter may tidy up the whole chat
        if (message.UserId != userId && invite.InviterUserId != userId)
        {
            return InviteChatOutcome.NotAllowed;
        }

        dbContext.InviteMessages.Remove(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return InviteChatOutcome.Done;
    }

    public async Task<int> DeleteOldChatsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var before = nowUtc.AddDays(-InviteChatRules.KeepDaysAfterVisit);
        var old = await dbContext.InviteMessages
            .Where(m => m.Invite.StartUtc < before)
            .Take(1000)
            .ToListAsync(cancellationToken);
        dbContext.InviteMessages.RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken);
        return old.Count;
    }

    // The chat as the viewer sees it: names, "mine", reactions with "Du" for the viewer
    private async Task<IReadOnlyList<InviteChatLineDto>> ChatAsync(BookingInvite invite, string viewerUserId,
        bool viewerIsInviter, CancellationToken cancellationToken)
    {
        var lines = await dbContext.InviteMessages.AsNoTracking()
            .Where(m => m.InviteId == invite.Id && m.Kind == InviteMessageKind.Chat)
            .OrderBy(m => m.CreatedUtc)
            .Select(m => new
            {
                m.Id, m.UserId, m.Text, m.CreatedUtc,
                Reactions = m.Reactions.OrderBy(r => r.CreatedUtc).Select(r => new { r.Kind, r.UserId }).ToList()
            })
            .ToListAsync(cancellationToken);
        var names = await FirstNamesAsync(
            lines.Select(l => l.UserId).Concat(lines.SelectMany(l => l.Reactions.Select(r => (string?)r.UserId)))
                .ToList(), cancellationToken);
        string NameOf(string? id) => id == viewerUserId ? Me : names.GetValueOrDefault(id ?? "", "En venn");

        return lines.Select(l => new InviteChatLineDto(
                l.Id,
                names.GetValueOrDefault(l.UserId ?? "", "En venn"),
                l.Text,
                l.UserId == viewerUserId,
                l.UserId == viewerUserId || viewerIsInviter,
                l.CreatedUtc,
                l.Reactions.GroupBy(r => r.Kind)
                    .Select(g => new InviteReactionDto(g.Key, g.Select(r => NameOf(r.UserId)).ToList()))
                    .ToList()))
            .ToList();
    }

    // "Skriv en liten hilsen": what the friends wrote with their answer, newest last (only for the inviter)
    private async Task<IReadOnlyList<FriendMessageDto>> GreetingsAsync(Guid inviteId,
        CancellationToken cancellationToken)
    {
        var greetings = await dbContext.InviteMessages.AsNoTracking()
            .Where(m => m.InviteId == inviteId && m.Kind == InviteMessageKind.Hilsen)
            .OrderBy(m => m.CreatedUtc)
            .Select(m => new { m.UserId, m.Text, m.Icon, m.Joined })
            .ToListAsync(cancellationToken);
        var names = await FirstNamesAsync(greetings.Select(g => g.UserId).ToList(), cancellationToken);
        return greetings.Select(g => new FriendMessageDto(names.GetValueOrDefault(g.UserId ?? "", "En venn"),
            g.Text, g.Icon, g.Joined)).ToList();
    }

    // Only the inviter and the friends who joined: invite = null and members = null means a wrong link,
    // invite = null with members set means "not yours to read"
    private async Task<(BookingInvite? Invite, List<string>? Members)> ChatAccessAsync(string token,
        string userId, CancellationToken cancellationToken)
    {
        var invite = await FindByTokenAsync(token, cancellationToken);
        if (invite is null)
        {
            return (null, null);
        }

        var members = await MemberIdsAsync(invite.Id, cancellationToken);
        return invite.InviterUserId == userId || members.Contains(userId) ? (invite, members) : (null, members);
    }

    private static InviteResult? GreetingError(string? greeting, string? icon)
    {
        if (greeting is { Length: > InviteChatRules.MaxGreetingLength })
        {
            return Invalid($"Hilsenen kan være maks {InviteChatRules.MaxGreetingLength} tegn.");
        }

        return string.IsNullOrEmpty(icon) || InviteChatRules.Reactions.Contains(icon)
            ? null
            : Invalid("Ukjent ikon.");
    }

    private void AddGreeting(BookingInvite invite, string? userId, string? greeting, string? icon, bool joined)
    {
        if (string.IsNullOrWhiteSpace(greeting))
        {
            return;
        }

        dbContext.InviteMessages.Add(new InviteMessage
        {
            Id = Guid.NewGuid(),
            InviteId = invite.Id,
            UserId = userId,
            Kind = InviteMessageKind.Hilsen,
            Text = greeting.Trim(),
            Icon = string.IsNullOrEmpty(icon) ? null : icon,
            Joined = joined,
            CreatedUtc = DateTime.UtcNow
        });
    }

    // Marks the chat bells of one invitation (the 10-minute pause looks for it); Min BESTI opens
    private static string ChatLink(Guid inviteId) => $"/min-besti?chat={inviteId}";
}
