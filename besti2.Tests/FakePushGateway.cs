using besti2.Application.Notifications;

namespace besti2.Tests;

// Remembers the push messages instead of sending them; Gone lists devices that "were uninstalled"
public class FakePushGateway(string? publicKey = "test-public-key") : IPushGateway
{
    public List<(string Endpoint, string Payload)> Sent { get; } = [];
    public HashSet<string> Gone { get; } = [];

    public string? PublicKey { get; } = publicKey;

    public Task<PushResult> SendAsync(string endpoint, string p256dh, string auth, string payloadJson,
        CancellationToken cancellationToken = default)
    {
        if (Gone.Contains(endpoint))
        {
            return Task.FromResult(PushResult.Gone);
        }
        Sent.Add((endpoint, payloadJson));
        return Task.FromResult(PushResult.Sent);
    }
}
