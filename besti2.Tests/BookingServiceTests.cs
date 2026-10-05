using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

public class BookingServiceTests
{
    // Poniedziałek 7.01.2030, 09:00 UTC = 10:00 w Oslo (czas zimowy)
    private static readonly DateTime FutureMondayAt10 = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static CreateBookingRequest HerreklippHosIngrid(string email = "kari@test.example") => new(
        new Guid("a1111111-0000-0000-0000-000000000002"),
        new Guid("b1111111-0000-0000-0000-000000000001"),
        FutureMondayAt10,
        "Kari Nordmann",
        "+4790000000",
        email,
        false);

    [Fact]
    public async Task CreateAsync_SameSlotTwice_SecondIsSlotTaken()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db);

        var first = await service.CreateAsync(HerreklippHosIngrid());
        var second = await service.CreateAsync(HerreklippHosIngrid());

        Assert.Equal(CreateBookingOutcome.Created, first.Outcome);
        Assert.Equal("Venter", first.Booking!.Status);
        Assert.Equal(CreateBookingOutcome.SlotTaken, second.Outcome);
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ReturnsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db);

        var result = await service.CreateAsync(HerreklippHosIngrid(email: "abc"));

        Assert.Equal(CreateBookingOutcome.Invalid, result.Outcome);
        Assert.Equal(0, await db.Bookings.CountAsync());
    }
}