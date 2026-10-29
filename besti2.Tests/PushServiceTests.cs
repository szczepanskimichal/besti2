using besti2.Application.Notifications;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// "Varsler på telefonen": new bell notifications go out as push messages to the right devices
public class PushServiceTests
{
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private const string Phone = "https://fcm.googleapis.com/fcm/send/abc123";
    private const string IPhone = "https://web.push.apple.com/QGx-abc";
    private const string P256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM";
    private const string Auth = "tBHItJI5svbpez7KI4CCXg";

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static PushService NewService(AppDbContext db, FakePushGateway gateway) =>
        new(db, gateway, NullLogger<PushService>.Instance);

    private static Notification NewNotification(string? userId, Guid? businessId, DateTime createdUtc) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        BusinessId = businessId,
        Text = "Ny bestilling: Klipp dame i morgen kl. 10:00",
        Link = "/panel",
        CreatedUtc = createdUtc
    };

    // check if a new business notification reaches the owner's phone once, and a customer's only the customer
    [Fact]
    public async Task SendPendingAsync_SendsEachNewNotificationOnceToTheRightDevice()
    {
        await using var db = await CreateDbAsync();
        var gateway = new FakePushGateway();
        var service = NewService(db, gateway);
        Assert.Null(await service.SubscribeAsync("eier-1", KlippOgStil, new PushSubscribeRequest(Phone, P256dh, Auth)));
        Assert.Null(await service.SubscribeAsync("kunde-1", null, new PushSubscribeRequest(IPhone, P256dh, Auth)));
        var forBusiness = NewNotification(null, KlippOgStil, Now.AddSeconds(-5));
        db.Notifications.AddRange(forBusiness, NewNotification("kunde-2", null, Now.AddSeconds(-5)));
        await db.SaveChangesAsync();

        var first = await service.SendPendingAsync(Now);
        var second = await service.SendPendingAsync(Now.AddSeconds(30));

        Assert.Equal(2, first);
        Assert.Equal(0, second);
        var sent = Assert.Single(gateway.Sent);
        Assert.Equal(Phone, sent.Endpoint);
        Assert.Contains($"/varsler/{forBusiness.Id}", sent.Payload);
        Assert.Contains("Ny bestilling", sent.Payload);
    }

    // check if an old notification (API was down) or one already read is not pushed
    [Fact]
    public async Task SendPendingAsync_SkipsOldAndRead()
    {
        await using var db = await CreateDbAsync();
        var gateway = new FakePushGateway();
        var service = NewService(db, gateway);
        await service.SubscribeAsync("kunde-1", null, new PushSubscribeRequest(Phone, P256dh, Auth));
        var read = NewNotification("kunde-1", null, Now.AddMinutes(-1));
        read.ReadUtc = Now;
        db.Notifications.AddRange(read, NewNotification("kunde-1", null, Now.AddHours(-2)));
        await db.SaveChangesAsync();

        await service.SendPendingAsync(Now);

        Assert.Empty(gateway.Sent);
    }

    // check if a device the push service says is gone (app removed) is forgotten
    [Fact]
    public async Task SendPendingAsync_ForgetsGoneDevice()
    {
        await using var db = await CreateDbAsync();
        var gateway = new FakePushGateway();
        gateway.Gone.Add(Phone);
        var service = NewService(db, gateway);
        await service.SubscribeAsync("kunde-1", null, new PushSubscribeRequest(Phone, P256dh, Auth));
        db.Notifications.Add(NewNotification("kunde-1", null, Now));
        await db.SaveChangesAsync();

        await service.SendPendingAsync(Now);

        Assert.Empty(await db.WebPushSubscriptions.ToListAsync());
    }

    // check if only real push services are accepted (the server must never call any address it is given)
    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/x", true)]
    [InlineData("https://web.push.apple.com/x", true)]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/x", true)]
    [InlineData("https://wns2-db5p.notify.windows.com/w/?token=x", true)]
    [InlineData("http://fcm.googleapis.com/fcm/send/x", false)]
    [InlineData("https://evil.example/fcm.googleapis.com", false)]
    [InlineData("https://fcm.googleapis.com.evil.example/x", false)]
    [InlineData("https://localhost:5156/api", false)]
    [InlineData("https://169.254.169.254/latest", false)]
    public async Task SubscribeAsync_OnlyKnownPushServices(string endpoint, bool allowed)
    {
        await using var db = await CreateDbAsync();
        var service = NewService(db, new FakePushGateway());

        var error = await service.SubscribeAsync("kunde-1", null, new PushSubscribeRequest(endpoint, P256dh, Auth));

        Assert.Equal(allowed, error is null);
    }

    // check if the same device moves to the person who logged in last, and unsubscribing removes it
    [Fact]
    public async Task SubscribeAsync_SameDeviceMovesToNewUser()
    {
        await using var db = await CreateDbAsync();
        var service = NewService(db, new FakePushGateway());

        await service.SubscribeAsync("kunde-1", null, new PushSubscribeRequest(Phone, P256dh, Auth));
        await service.SubscribeAsync("kunde-2", null, new PushSubscribeRequest(Phone, P256dh, Auth));

        var device = Assert.Single(await db.WebPushSubscriptions.ToListAsync());
        Assert.Equal("kunde-2", device.UserId);

        await service.UnsubscribeAsync("kunde-1", Phone); // not hers any more: nothing happens
        Assert.Single(await db.WebPushSubscriptions.ToListAsync());
        await service.UnsubscribeAsync("kunde-2", Phone);
        Assert.Empty(await db.WebPushSubscriptions.ToListAsync());
    }

    // check if push without VAPID keys on the server is simply off
    [Fact]
    public async Task WithoutKeys_PushIsOff()
    {
        await using var db = await CreateDbAsync();
        var service = NewService(db, new FakePushGateway(publicKey: null));

        var error = await service.SubscribeAsync("kunde-1", null, new PushSubscribeRequest(Phone, P256dh, Auth));

        Assert.Null(service.PublicKey);
        Assert.NotNull(error);
        Assert.Equal(0, await service.SendPendingAsync(Now));
    }
}
