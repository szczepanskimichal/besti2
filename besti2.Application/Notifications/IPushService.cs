namespace besti2.Application.Notifications;

// "Varsler på telefonen": the bell's notifications also as push messages (Web Push), free and without an app
// store. Works in Chrome, Edge, Firefox and Safari, on iPhone only when BESTI is added to the home screen
public interface IPushService
{
    // For the browser's subscribe call; null = push is not set up on this server (no VAPID keys)
    string? PublicKey { get; }

    // Saves this device for the user (and the business, for an owner); returns an error text or null
    Task<string?> SubscribeAsync(string userId, Guid? businessId, PushSubscribeRequest request,
        CancellationToken cancellationToken = default);

    Task UnsubscribeAsync(string userId, string endpoint, CancellationToken cancellationToken = default);

    // Sends the new notifications not sent yet; returns how many were looked at
    Task<int> SendPendingAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}

// What the browser's pushManager.subscribe() gives (keys in base64url)
public record PushSubscribeRequest(string Endpoint, string P256dh, string Auth);

// One push message to one device. Gone = the device unsubscribed or the app was removed: forget it
public enum PushResult
{
    Sent,
    Gone,
    Failed
}

// Talks to the push service (Google, Apple, Mozilla, Microsoft); a fake one in the tests
public interface IPushGateway
{
    string? PublicKey { get; }

    Task<PushResult> SendAsync(string endpoint, string p256dh, string auth, string payloadJson,
        CancellationToken cancellationToken = default);
}
