using besti2.Application.Bookings;
using besti2.Application.Reviews;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class NotificationTests
{
    // Monday 5 Jan 2099 (Klipp & Stil is open 09–17); far in the future, so the test never gets old
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private const string Kari = "kari-user";

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static BookingService Bookings(AppDbContext db) =>
        new(db, new FakeEmailService(), NullLogger<BookingService>.Instance);

    private static async Task<Guid> KariBooksAsync(BookingService service)
    {
        var result = await service.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId,
            SlotCalculator.ToUtc(FutureMonday, new TimeOnly(10, 0)), "Kari Nordmann", "+4790000000",
            "kari@test.example", false), Kari);
        return result.Booking!.Id;
    }

    // check if a new online booking rings the business's bell with a link to the visit in the calendar
    [Fact]
    public async Task NewBooking_NotifiesTheBusiness()
    {
        await using var db = await CreateSeededDbAsync();
        var id = await KariBooksAsync(Bookings(db));

        var bell = await new NotificationService(db).GetAsync("owner-user", KlippOgStil);

        Assert.Equal(1, bell.Unread);
        var item = Assert.Single(bell.Items);
        Assert.Contains("Ny bestilling: Kari Nordmann", item.Text);
        Assert.Equal($"/panel?dag=2099-01-05&bestilling={id}", item.Link);
    }

    // check if the customer is told when the business confirms or cancels (with the business's message)
    [Fact]
    public async Task ConfirmAndCancel_NotifyTheCustomer()
    {
        await using var db = await CreateSeededDbAsync();
        var bookings = Bookings(db);
        var id = await KariBooksAsync(bookings);

        await bookings.UpdateStatusAsync(id, KlippOgStil, BookingStatus.Bekreftet);
        await bookings.UpdateStatusAsync(id, KlippOgStil, BookingStatus.Avlyst, "Beklager, Ingrid er syk.");

        var bell = await new NotificationService(db).GetAsync(Kari, null);
        Assert.Equal(2, bell.Unread);
        Assert.Contains("Klipp & Stil Frisør har avlyst timen", bell.Items[0].Text);
        Assert.EndsWith(": Beklager, Ingrid er syk.", bell.Items[0].Text);
        Assert.Contains("har bekreftet timen", bell.Items[1].Text);
    }

    // check if a moved visit that waits for an answer asks the customer, and the answer reaches the business
    [Fact]
    public async Task MoveAndAnswer_NotifyBothSides()
    {
        await using var db = await CreateSeededDbAsync();
        var bookings = Bookings(db);
        var id = await KariBooksAsync(bookings);
        await bookings.MoveAsync(id, KlippOgStil,
            new MoveBookingRequest(IngridId, FutureMonday, new TimeOnly(13, 0), false, AskCustomer: true));

        await bookings.AnswerNewTimeByCustomerAsync(id, Kari, accept: true);

        var customer = await new NotificationService(db).GetAsync(Kari, null);
        var business = await new NotificationService(db).GetAsync("owner-user", KlippOgStil);
        Assert.Contains("ønsker å flytte timen", customer.Items[0].Text);
        Assert.Equal("/min-besti", customer.Items[0].Link);
        Assert.Contains("har godtatt ny tid", business.Items[0].Text);
    }

    // check if a walk-in without an account gets nothing, and another business sees nothing
    [Fact]
    public async Task OnlyTheRightBell()
    {
        await using var db = await CreateSeededDbAsync();
        var bookings = Bookings(db);
        var walkIn = await bookings.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(12, 0), "Ola", "912 34 567", "ola@test.example", null, false));
        await KariBooksAsync(bookings);

        await bookings.UpdateStatusAsync(walkIn.Booking!.Id, KlippOgStil, BookingStatus.Avlyst);

        Assert.Equal(0, await db.Notifications.CountAsync(n => n.UserId != null));
        Assert.Empty((await new NotificationService(db).GetAsync("other-owner", FjordBygg)).Items);
    }
    
    // check if a click reads only that one notification and returns where it leads
    [Fact]
    public async Task MarkRead_OnlyThatOne()
    {
        await using var db = await CreateSeededDbAsync();
        var bookings = Bookings(db);
        var id = await KariBooksAsync(bookings);
        await bookings.CancelByCustomerAsync(id, Kari); // a second notification for the business
        var notifications = new NotificationService(db);
        var before = await notifications.GetAsync("owner-user", KlippOgStil);

        var read = await notifications.MarkReadAsync("owner-user", KlippOgStil, before.Items[1].Id);

        var after = await notifications.GetAsync("owner-user", KlippOgStil);
        Assert.Equal(before.Items[1].Link, read!.Link);
        Assert.Equal(1, after.Unread);
        Assert.Equal(2, after.Items.Count);
        Assert.False(after.Items[0].IsRead);
        Assert.True(after.Items[1].IsRead);
    }

    // check if "Merk alle som lest" keeps the notifications, and the bin removes only one, only your own
    [Fact]
    public async Task MarkAllAndDelete()
    {
        await using var db = await CreateSeededDbAsync();
        var bookings = Bookings(db);
        var id = await KariBooksAsync(bookings);
        await bookings.CancelByCustomerAsync(id, Kari);
        var notifications = new NotificationService(db);
        await notifications.MarkAllReadAsync("owner-user", KlippOgStil);
        var items = (await notifications.GetAsync("owner-user", KlippOgStil)).Items;

        var byOther = await notifications.DeleteAsync("other-owner", FjordBygg, items[0].Id);
        var byOwner = await notifications.DeleteAsync("owner-user", KlippOgStil, items[0].Id);

        var after = await notifications.GetAsync("owner-user", KlippOgStil);
        Assert.False(byOther);
        Assert.True(byOwner);
        Assert.Equal(0, after.Unread);
        Assert.Single(after.Items);
    }

    // check if old notifications disappear by themselves: 7 days for a customer, 14 days for a business
    [Fact]
    public async Task OldNotifications_AreRemoved()
    {
        await using var db = await CreateSeededDbAsync();
        Domain.Entities.Notification Note(int daysAgo, string? userId, Guid? businessId) => new()
        {
            Id = Guid.NewGuid(), UserId = userId, BusinessId = businessId, Text = $"{daysAgo} dager",
            CreatedUtc = DateTime.UtcNow.AddDays(-daysAgo)
        };
        db.Notifications.AddRange(Note(6, Kari, null), Note(8, Kari, null), Note(13, null, KlippOgStil),
            Note(15, null, KlippOgStil));
        await db.SaveChangesAsync();
        var notifications = new NotificationService(db);

        var customer = await notifications.GetAsync(Kari, null);
        var business = await notifications.GetAsync("owner-user", KlippOgStil);

        Assert.Equal("6 dager", Assert.Single(customer.Items).Text);
        Assert.Equal("13 dager", Assert.Single(business.Items).Text);
    }
    // check if a new review rings the business's bell
    [Fact]
    public async Task NewReview_NotifiesTheBusiness()
    {
        await using var db = await CreateSeededDbAsync();
        var booking = new Domain.Entities.Booking
        {
            Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            CustomerName = "Kari Nordmann", CustomerPhone = "90000000", CustomerEmail = "kari@test.example",
            Status = BookingStatus.Bekreftet, CustomerUserId = Kari, EmployeeId = IngridId, ServiceId = HerreklippId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        await new ReviewService(db).CreateAsync(Kari, new CreateReviewRequest(booking.Id, 5, null));

        var bell = await new NotificationService(db).GetAsync("owner-user", KlippOgStil);
        Assert.Equal("Ny vurdering: 5 av 5 stjerner fra Kari", bell.Items.Single().Text);
        Assert.Equal("/panel/vurderinger", bell.Items.Single().Link);
    }
    // check if a saved notification rings that bell's live connection at once, and nobody else's
    [Fact]
    public async Task NewNotification_SignalsOnlyThatBell()
    {
        await using var db = await CreateSeededDbAsync();
        using var business = NotificationSignals.Subscribe(NotificationSignals.Key(null, KlippOgStil));
        using var stranger = NotificationSignals.Subscribe(NotificationSignals.Key("nobody-" + Guid.NewGuid(), null));

        await KariBooksAsync(Bookings(db));

        Assert.True(business.Reader.TryRead(out _));
        Assert.False(stranger.Reader.TryRead(out _));
    }
    
}
