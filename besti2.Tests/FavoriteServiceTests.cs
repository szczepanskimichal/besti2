using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Favoritter": the heart on the business page and the list in Min Besti
public class FavoriteServiceTests
{
    private static readonly DateTime Now = new(2030, 1, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");
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

    // check if a heart is saved once, shows the last visit for "Bestill igjen" and can be removed
    [Fact]
    public async Task AddGetRemove_WithLastVisit()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(), StartUtc = Now.AddDays(-20), EndUtc = Now.AddDays(-20).AddMinutes(30),
            CreatedUtc = Now.AddDays(-25), CustomerName = "Kari", CustomerPhone = "+4790000000",
            CustomerEmail = "kari@test.example", CustomerUserId = "kari", Status = BookingStatus.Bekreftet,
            ServiceId = Herreklipp, EmployeeId = Ingrid
        });
        await db.SaveChangesAsync();
        var service = new FavoriteService(db, new FakeImageStorage());

        Assert.Null(await service.AddAsync("kari", KlippOgStil, Now));
        Assert.Null(await service.AddAsync("kari", KlippOgStil, Now)); // twice is fine
        Assert.Null(await service.AddAsync("kari", FjordBygg, Now.AddMinutes(1)));
        var mine = await service.GetMineAsync("kari", Now);
        await service.RemoveAsync("kari", FjordBygg);

        Assert.Equal(2, mine.Count);
        Assert.Equal(FjordBygg, mine[0].BusinessId); // newest first
        Assert.Null(mine[0].LastVisit);
        Assert.Equal(Herreklipp, mine[1].LastVisit!.ServiceId);
        Assert.Equal(Ingrid, mine[1].LastVisit!.EmployeeId);
        Assert.True(mine[1].LastVisit!.ServiceAvailable);
        Assert.Equal([KlippOgStil], await service.GetIdsAsync("kari"));
        Assert.Empty(await service.GetIdsAsync("ola"));
    }

    // check if a hidden business (not approved yet) cannot be saved
    [Fact]
    public async Task AddAsync_HiddenBusiness_IsRefused()
    {
        await using var db = await CreateSeededDbAsync();
        (await db.Businesses.FindAsync(KlippOgStil))!.IsHidden = true;
        await db.SaveChangesAsync();

        var error = await new FavoriteService(db, new FakeImageStorage()).AddAsync("kari", KlippOgStil, Now);

        Assert.Equal("Fant ikke bedriften.", error);
        Assert.Empty(db.Favorites);
    }
}
