using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// "Si fra hvis det blir ledig": waiting customers hear about a NEW free time once, at the same moment
public class WaitlistServiceTests
{
    // Monday 5 Jan 2099, far in the future so the test never gets old (Klipp & Stil is open)
    private static readonly DateOnly Monday = new(2099, 1, 5);
    private static readonly DateTime Now = new(2099, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc); // 10:00 in Oslo
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

    private static WaitlistService NewService(AppDbContext db) =>
        new(db, new AvailabilityService(db), NullLogger<WaitlistService>.Instance);

    private static Booking BookingAt(DateTime startUtc) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = Now,
        CustomerName = "Ola",
        CustomerPhone = "+4790000000",
        CustomerEmail = "ola@test.example",
        Status = BookingStatus.Bekreftet,
        ServiceId = Herreklipp,
        EmployeeId = Ingrid
    };

    // check if a cancelled visit tells the waiting customers once, with the time and a link to book it
    [Fact]
    public async Task CheckAsync_CancelledVisit_TellsWaitingCustomersOnce()
    {
        await using var db = await CreateSeededDbAsync();
        var booked = BookingAt(MondayAt10);
        db.Bookings.Add(booked);
        await db.SaveChangesAsync();
        var service = NewService(db);
        Assert.Null(await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, Ingrid, Monday), Now));
        Assert.Null(await service.JoinAsync("kunde-2", new JoinWaitlistRequest(Herreklipp, Ingrid, Monday), Now));

        // Nothing new yet: the times free when they joined are no news
        Assert.Equal(0, await service.CheckAsync(Now));

        booked.Status = BookingStatus.Avlyst;
        await db.SaveChangesAsync();
        var told = await service.CheckAsync(Now.AddMinutes(2));
        var again = await service.CheckAsync(Now.AddMinutes(4));

        Assert.Equal(2, told);
        Assert.Equal(0, again);
        var notifications = await db.Notifications.OrderBy(n => n.UserId).ToListAsync();
        Assert.Equal(["kunde-1", "kunde-2"], notifications.Select(n => n.UserId));
        Assert.Contains("kl. 10:00", notifications[0].Text);
        Assert.Contains("Klipp & Stil", notifications[0].Text);
        Assert.Contains($"tjeneste={Herreklipp}", notifications[0].Link);
        Assert.Contains("dato=2099-01-05", notifications[0].Link);
        Assert.Contains($"ansatt={Ingrid}", notifications[0].Link);
    }

    // check if days that have passed are removed
    [Fact]
    public async Task CheckAsync_RemovesPastDays()
    {
        await using var db = await CreateSeededDbAsync();
        var service = NewService(db);
        await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, Monday), Now);

        await service.CheckAsync(MondayAt10.AddDays(1));

        Assert.Empty(await db.WaitlistEntries.ToListAsync());
    }

    // check if the same wish twice is one entry, and at most five days at a time
    [Fact]
    public async Task JoinAsync_NoDuplicatesAndAtMostFive()
    {
        await using var db = await CreateSeededDbAsync();
        var service = NewService(db);

        for (var i = 0; i < WaitlistService.MaxPerCustomer; i++)
        {
            Assert.Null(await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, Monday.AddDays(i)), Now));
        }
        Assert.Null(await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, Monday), Now));
        var tooMany = await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, Monday.AddDays(9)), Now);

        Assert.NotNull(tooMany);
        Assert.Equal(WaitlistService.MaxPerCustomer, await db.WaitlistEntries.CountAsync());
        Assert.Equal(WaitlistService.MaxPerCustomer, (await service.GetMineAsync("kunde-1", Now)).Count);
    }

    // check if a day in the past, too far ahead or a wrong service is refused
    [Fact]
    public async Task JoinAsync_RefusesWrongDayOrService()
    {
        await using var db = await CreateSeededDbAsync();
        var service = NewService(db);
        var today = DateOnly.FromDateTime(Now);

        Assert.NotNull(await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, today.AddDays(-1)), Now));
        Assert.NotNull(await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, today.AddDays(61)), Now));
        Assert.NotNull(await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Guid.NewGuid(), null, Monday), Now));
        Assert.NotNull(await service.JoinAsync("kunde-1",
            new JoinWaitlistRequest(Herreklipp, new Guid("b2222222-0000-0000-0000-000000000001"), Monday), Now));
    }

    // check if a customer can leave the list, but not remove someone else's entry
    [Fact]
    public async Task LeaveAsync_OnlyOwnEntry()
    {
        await using var db = await CreateSeededDbAsync();
        var service = NewService(db);
        await service.JoinAsync("kunde-1", new JoinWaitlistRequest(Herreklipp, null, Monday), Now);
        var id = (await service.GetMineAsync("kunde-1", Now))[0].Id;

        Assert.False(await service.LeaveAsync("kunde-2", id));
        Assert.True(await service.LeaveAsync("kunde-1", id));
        Assert.Empty(await db.WaitlistEntries.ToListAsync());
    }
}
