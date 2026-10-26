using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class FreeDaysTests
{
    // Saturday 3 Jan 2099 (Klipp & Stil 10–15, Sunday closed); far in the future, so the test never gets old
    private static readonly DateOnly FutureSaturday = new(2099, 1, 3);
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);
    private static readonly DateTime FutureMondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc); // 10:00 in Oslo

    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid SofieId = new("b1111111-0000-0000-0000-000000000002");

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

    // check if we get 7 days in a row and a closed Sunday is there, but empty (greyed out in the strip)
    [Fact]
    public async Task GetFreeDaysAsync_ReturnsSevenDays_ClosedSundayIsEmpty()
    {
        await using var db = await CreateSeededDbAsync();

        var days = await Bookings(db).GetFreeDaysAsync(HerreklippId, null, FutureSaturday, 7);

        Assert.NotNull(days);
        Assert.Equal(7, days.Count);
        Assert.Equal(FutureSaturday, days[0].Date);
        Assert.NotEmpty(days[0].Slots);
        Assert.Empty(days[1].Slots); // Sunday
    }

    // check if "Hvem som helst" still offers 10:00 when Ingrid is busy, because Sofie is free
    [Fact]
    public async Task GetFreeDaysAsync_AnyEmployee_OffersTimeWhenOnlyOneIsBusy()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Bookings(db);
        await service.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId, FutureMondayAt10,
            "Kari Nordmann", "+4790000000", "kari@test.example", false));

        var anyone = await service.GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1);
        var onlyIngrid = await service.GetFreeDaysAsync(HerreklippId, IngridId, FutureMonday, 1);

        var tenOClock = Assert.Single(anyone![0].Slots, s => s.StartUtc == FutureMondayAt10);
        Assert.Equal(SofieId, tenOClock.EmployeeId);
        Assert.DoesNotContain(onlyIngrid![0].Slots, s => s.StartUtc == FutureMondayAt10);
    }

    // check if an unknown employee gives null (= 404), not someone else's times
    [Fact]
    public async Task GetFreeDaysAsync_UnknownEmployee_ReturnsNull()
    {
        await using var db = await CreateSeededDbAsync();

        var days = await Bookings(db).GetFreeDaysAsync(HerreklippId, Guid.NewGuid(), FutureMonday, 7);

        Assert.Null(days);
    }
}