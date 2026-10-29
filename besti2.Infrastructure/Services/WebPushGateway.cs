using System.Net;
using besti2.Application.Notifications;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// Sends one push message through the browser's push service, signed with BESTI's VAPID keys
// (Push:PublicKey and Push:PrivateKey; without them push is simply off)
public class WebPushGateway : IPushGateway
{
    private readonly PushServiceClient? client;
    private readonly ILogger<WebPushGateway> logger;

    public WebPushGateway(HttpClient httpClient, string? publicKey, string? privateKey, string subject,
        ILogger<WebPushGateway> logger)
    {
        this.logger = logger;
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            return;
        }
        PublicKey = publicKey;
        client = new PushServiceClient(httpClient)
        {
            DefaultAuthentication = new VapidAuthentication(publicKey, privateKey) { Subject = subject }
        };
    }

    public string? PublicKey { get; }

    public async Task<PushResult> SendAsync(string endpoint, string p256dh, string auth, string payloadJson,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            return PushResult.Failed;
        }

        var subscription = new PushSubscription { Endpoint = endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, p256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, auth);
        // Kept by the push service for a day while the phone is off; "high" wakes a sleeping phone
        var message = new PushMessage(payloadJson) { TimeToLive = 24 * 60 * 60, Urgency = PushMessageUrgency.High };
        try
        {
            await client.RequestPushMessageDeliveryAsync(subscription, message, cancellationToken);
            return PushResult.Sent;
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return PushResult.Gone;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Push service {Host} refused the message", new Uri(endpoint).Host);
            return PushResult.Failed;
        }
    }
}
