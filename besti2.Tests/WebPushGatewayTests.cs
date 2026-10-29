using System.Net;
using System.Security.Cryptography;
using besti2.Application.Notifications;
using besti2.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// The real sender, with a fake push service: signed with VAPID and encrypted the way Apple and Google want
public class WebPushGatewayTests
{
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // An uncompressed P-256 public key (65 bytes) and the private number (32 bytes), like web-push makes them
    private static (string PublicKey, string PrivateKey) NewKeys()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = key.ExportParameters(true);
        return (Base64Url([0x04, .. p.Q.X!, .. p.Q.Y!]), Base64Url(p.D!));
    }

    // check if the message is signed (VAPID), encrypted (aes128gcm) and "gone" devices are recognised
    [Theory]
    [InlineData(HttpStatusCode.Created, PushResult.Sent)]
    [InlineData(HttpStatusCode.Gone, PushResult.Gone)]
    [InlineData(HttpStatusCode.NotFound, PushResult.Gone)]
    [InlineData(HttpStatusCode.InternalServerError, PushResult.Failed)]
    public async Task SendAsync_SignedEncryptedAndReadsAnswer(HttpStatusCode answer, PushResult expected)
    {
        var server = NewKeys();
        var device = NewKeys();
        var handler = new RecordingHandler(answer);
        var gateway = new WebPushGateway(new HttpClient(handler), server.PublicKey, server.PrivateKey,
            "mailto:post@bestiapp.no", NullLogger<WebPushGateway>.Instance);

        var result = await gateway.SendAsync("https://fcm.googleapis.com/fcm/send/abc", device.PublicKey,
            Base64Url(RandomNumberGenerator.GetBytes(16)), """{"title":"BESTI","body":"Hei"}""");

        Assert.Equal(expected, result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://fcm.googleapis.com/fcm/send/abc", request.RequestUri!.ToString());
        Assert.StartsWith("vapid", request.Headers.Authorization!.Scheme);
        Assert.Contains("aes128gcm", request.Content!.Headers.ContentEncoding);
    }

    // check if push is off without keys
    [Fact]
    public void WithoutKeys_NoPublicKey()
    {
        var gateway = new WebPushGateway(new HttpClient(), null, null, "mailto:post@bestiapp.no",
            NullLogger<WebPushGateway>.Instance);

        Assert.Null(gateway.PublicKey);
    }

    private sealed class RecordingHandler(HttpStatusCode answer) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(answer));
        }
    }
}
