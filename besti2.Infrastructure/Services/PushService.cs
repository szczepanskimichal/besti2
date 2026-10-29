using System.Text.Json;
using besti2.Application.Notifications;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

public class PushService(AppDbContext dbContext, IPushGateway gateway, ILogger<PushService> logger) : IPushService
{
    public const int BatchSize = 50;

    // A phone, a tablet and a computer or two is plenty; the oldest one goes when there are more
    public const int MaxDevicesPerUser = 10;

    // Older notifications are not pushed any more (e.g. after the API was down): the bell still has them
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(15);

    // The server only ever calls these push services. Anything else could make BESTI call any address (SSRF)
    private static readonly string[] AllowedHosts =
        ["fcm.googleapis.com", "push.apple.com", "push.services.mozilla.com", "notify.windows.com"];

    public string? PublicKey => gateway.PublicKey;

    public async Task<string?> SubscribeAsync(string userId, Guid? businessId, PushSubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (PublicKey is null)
        {
            return "Varsler på telefonen er ikke slått på hos BESTI ennå.";
        }
        if (!IsAllowedEndpoint(request.Endpoint) || !IsKey(request.P256dh, 200) || !IsKey(request.Auth, 100))
        {
            return "Nettleseren sendte en ugyldig adresse for varsler.";
        }

        // The same device again (or another person logged in on it): it now belongs to this user
        var device = await dbContext.WebPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, cancellationToken);
        if (device is null)
        {
            device = new WebPushSubscription { Id = Guid.NewGuid(), Endpoint = request.Endpoint };
            dbContext.WebPushSubscriptions.Add(device);
        }
        device.UserId = userId;
        device.BusinessId = businessId;
        device.P256dh = request.P256dh;
        device.Auth = request.Auth;
        device.CreatedUtc = DateTime.UtcNow;

        var others = await dbContext.WebPushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint != request.Endpoint)
            .OrderByDescending(s => s.CreatedUtc)
            .Skip(MaxDevicesPerUser - 1)
            .ToListAsync(cancellationToken);
        dbContext.WebPushSubscriptions.RemoveRange(others);
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task UnsubscribeAsync(string userId, string endpoint, CancellationToken cancellationToken = default)
    {
        var devices = await dbContext.WebPushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == endpoint)
            .ToListAsync(cancellationToken);
        dbContext.WebPushSubscriptions.RemoveRange(devices);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SendPendingAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (PublicKey is null)
        {
            return 0;
        }

        var since = nowUtc - MaxAge;
        var pending = await dbContext.Notifications
            .Where(n => n.PushedUtc == null && n.CreatedUtc >= since)
            .OrderBy(n => n.CreatedUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
        foreach (var notification in pending)
        {
            notification.PushedUtc = nowUtc;
            if (notification.ReadUtc is not null)
            {
                continue; // already seen in another tab
            }

            var devices = notification.BusinessId is Guid businessId
                ? dbContext.WebPushSubscriptions.Where(s => s.BusinessId == businessId)
                : dbContext.WebPushSubscriptions.Where(s => s.UserId == notification.UserId && s.BusinessId == null);
            // A click opens /varsler/{id}: marks it as read and goes on to what it is about
            var payload = JsonSerializer.Serialize(new
            {
                title = "BESTI",
                body = notification.Text,
                url = $"/varsler/{notification.Id}",
                tag = notification.Id
            });
            foreach (var device in await devices.ToListAsync(cancellationToken))
            {
                var result = await gateway.SendAsync(device.Endpoint, device.P256dh, device.Auth, payload,
                    cancellationToken);
                if (result == PushResult.Gone)
                {
                    dbContext.WebPushSubscriptions.Remove(device);
                }
                else if (result == PushResult.Failed)
                {
                    logger.LogWarning("Push message for notification {NotificationId} failed", notification.Id);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }

    private static bool IsAllowedEndpoint(string? endpoint) =>
        endpoint is { Length: <= 800 }
        && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.IsDefaultPort
        && AllowedHosts.Any(host => uri.Host == host || uri.Host.EndsWith("." + host, StringComparison.Ordinal));

    // base64url: letters, digits, - and _
    private static bool IsKey(string? key, int maxLength) =>
        key is { Length: > 0 } && key.Length <= maxLength
        && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '=');
}
