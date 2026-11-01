using besti2.Application.Loyalty;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Stempelkort" (every N-th visit gives a reward) and "Mitt BESTI-år"
public class LoyaltyAndYearTests
{
    private static readonly DateTime Now = new(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Herreklipp = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid Ingrid = new("b1111111-0000-0000-0000-000000000001");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Booking Visit(DateTime startUtc, string? userId, BookingStatus status = BookingStatus.Bekreftet,
        Guid? inviteId = null) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = startUtc.AddDays(-3),
        CustomerName = "Kari Nordmann",
        CustomerPhone = "+4790000000",
        CustomerEmail = "kari@test.example",
        CustomerUserId = userId,
        Status = status,
        PriceNok = 450m,
        InviteId = inviteId,
        ServiceId = Herreklipp,
        EmployeeId = Ingrid
    };

    // check if only visits with an account after the start count, and the reward starts a new round
    [Fact]
    public async Task StampCard_CountsVisitsAndRedeems()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new LoyaltyService(db);
        Assert.Null(await service.SetAsync(KlippOgStil, new SetLoyaltyRequest(5, "50 % på neste klipp"), Now.AddDays(-60)));
        db.Bookings.Add(Visit(Now.AddDays(-90), "kari")); // before the card started
        for (var i = 1; i <= 5; i++)
        {
            db.Bookings.Add(Visit(Now.AddDays(-i * 7), "kari"));
        }
        db.Bookings.Add(Visit(Now.AddDays(-3), "kari", BookingStatus.MotteIkke));
        db.Bookings.Add(Visit(Now.AddDays(-2), null)); // added by the business, no account
        db.Bookings.Add(Visit(Now.AddDays(2), "kari")); // not yet
        await db.SaveChangesAsync();

        var before = await service.GetForPanelAsync(KlippOgStil, Now);
        var cards = await service.GetMineAsync("kari", Now);
        var tooEarly = await service.RedeemAsync(KlippOgStil, "ola", Now);
        var redeemed = await service.RedeemAsync(KlippOgStil, "kari", Now);
        var twice = await service.RedeemAsync(KlippOgStil, "kari", Now);
        var after = await service.GetMineAsync("kari", Now);

        var kari = Assert.Single(before!.Customers);
        Assert.Equal(5, kari.Stamps);
        Assert.True(kari.RewardReady);
        Assert.Equal("Kari Nordmann", kari.Name);
        Assert.True(Assert.Single(cards).RewardReady);
        Assert.NotNull(tooEarly);
        Assert.Null(redeemed);
        Assert.NotNull(twice);
        Assert.Equal(0, after[0].Stamps);
        Assert.Equal(1, after[0].RewardsGiven);
    }

    // check if the settings are checked, and turning off hides the card
    [Fact]
    public async Task StampCard_SettingsAreChecked()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new LoyaltyService(db);

        Assert.NotNull(await service.SetAsync(KlippOgStil, new SetLoyaltyRequest(11, "Gratis klipp"), Now));
        Assert.NotNull(await service.SetAsync(KlippOgStil, new SetLoyaltyRequest(10, " "), Now));
        Assert.Null(await service.SetAsync(KlippOgStil, new SetLoyaltyRequest(10, "Gratis klipp"), Now));
        Assert.Null(await service.SetAsync(KlippOgStil, new SetLoyaltyRequest(null, null), Now));

        var card = await service.GetForPanelAsync(KlippOgStil, Now);
        Assert.Null(card!.Visits);
        Assert.Empty(await service.GetMineAsync("kari", Now));
    }

    // check if the year counts only visits that took place in that year, with favourites and friends
    [Fact]
    public async Task MyYear_CountsTheYear()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.AddRange(
            Visit(new DateTime(2030, 1, 10, 9, 0, 0, DateTimeKind.Utc), "kari"),
            Visit(new DateTime(2030, 3, 10, 9, 0, 0, DateTimeKind.Utc), "kari", inviteId: Guid.NewGuid()),
            Visit(new DateTime(2030, 3, 20, 9, 0, 0, DateTimeKind.Utc), "kari"),
            Visit(new DateTime(2030, 4, 1, 9, 0, 0, DateTimeKind.Utc), "kari", BookingStatus.Avlyst),
            Visit(new DateTime(2029, 12, 31, 23, 30, 0, DateTimeKind.Utc), "kari"), // 1 January 2030 in Oslo
            Visit(new DateTime(2028, 5, 1, 9, 0, 0, DateTimeKind.Utc), "kari"));
        await db.SaveChangesAsync();

        var year = await new MyYearService(db).GetAsync("kari", 2030, Now);

        Assert.Equal(4, year.Visits);
        Assert.Equal(1, year.Businesses);
        Assert.Equal(120, year.MinutesOfMeTime);
        Assert.Equal(1800m, year.Spent);
        Assert.Equal(1, year.VisitsWithFriends);
        Assert.Equal("Klipp & Stil Frisør", year.FavoriteBusiness);
        Assert.Equal("Herreklipp", year.FavoriteService);
        Assert.Equal(1, year.BusiestMonth); // January and March both 2: the first one
        Assert.Equal([2030, 2028], year.Years);
    }
}
