using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class BookingChangeTimeTests
{
    // Monday 5 Jan 2099 (Klipp & Stil is open 09–17); far in the future, so the test never gets old
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);

    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid SofieId = new("b1111111-0000-0000-0000-000000000002");
    private static readonly Guid LarsId = new("b2222222-0000-0000-0000-000000000001"); // Fjord Bygg
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

    private static DateTime Oslo(int hour, int minute = 0) =>
        SlotCalculator.ToUtc(FutureMonday, new TimeOnly(hour, minute));

    // Kari books Herreklipp with Ingrid online (Venter)
    private static async Task<Guid> BookAsync(BookingService service, int hour, string userId = Kari)
    {
        var result = await service.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId, Oslo(hour),
            "Kari Nordmann", "+4790000000", "kari@test.example", false), userId);
        return result.Booking!.Id;
    }

    // check if the customer can pick another free time with another employee; the business must confirm again
    [Fact]
    public async Task ChangeTime_ToFreeTime_MovesAndWaitsForTheBusiness()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var id = await BookAsync(service, 10);
        await service.UpdateStatusAsync(id, KlippOgStil, BookingStatus.Bekreftet);
        email.Sent.Clear();

        var result = await service.ChangeTimeByCustomerAsync(id, Kari, new ChangeTimeRequest(SofieId, Oslo(14)));

        var booking = await db.Bookings.SingleAsync(b => b.Id == id);
        Assert.Equal(MoveBookingOutcome.Moved, result.Outcome);
        Assert.Equal(SofieId, booking.EmployeeId);
        Assert.Equal(Oslo(14), booking.StartUtc);
        Assert.Equal(BookingStatus.Venter, booking.Status);
        var mail = Assert.Single(email.Sent); // to the business
        Assert.Contains("har endret tid", mail.Body);
        Assert.Contains("Tidligere tid: mandag 5. januar kl. 10:00", mail.Body);
    }

    // check if a time that is not free (another visit, outside opening hours) is refused
    [Fact]
    public async Task ChangeTime_NotFree_IsSlotTaken()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var mine = await BookAsync(service, 10);
        await BookAsync(service, 12, "someone-else");

        var onOther = await service.ChangeTimeByCustomerAsync(mine, Kari, new ChangeTimeRequest(IngridId, Oslo(12)));
        var evening = await service.ChangeTimeByCustomerAsync(mine, Kari, new ChangeTimeRequest(IngridId, Oslo(20)));

        Assert.Equal(MoveBookingOutcome.SlotTaken, onOther.Outcome);
        Assert.Equal(MoveBookingOutcome.SlotTaken, evening.Outcome);
        Assert.Equal(Oslo(10), (await db.Bookings.SingleAsync(b => b.Id == mine)).StartUtc);
    }

    // check if a customer cannot change someone else's booking or pick another business's employee
    [Fact]
    public async Task ChangeTime_NotOwnOrOtherBusiness_IsNotFound()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await BookAsync(service, 10);

        var byOther = await service.ChangeTimeByCustomerAsync(id, "someone-else", new ChangeTimeRequest(IngridId, Oslo(14)));
        var otherBusiness = await service.ChangeTimeByCustomerAsync(id, Kari, new ChangeTimeRequest(LarsId, Oslo(14)));

        Assert.Equal(MoveBookingOutcome.NotFound, byOther.Outcome);
        Assert.Equal(MoveBookingOutcome.NotFound, otherBusiness.Outcome);
    }

    // check if a cancelled booking cannot get a new time
    [Fact]
    public async Task ChangeTime_Cancelled_IsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await BookAsync(service, 10);
        await service.CancelByCustomerAsync(id, Kari);

        var result = await service.ChangeTimeByCustomerAsync(id, Kari, new ChangeTimeRequest(IngridId, Oslo(14)));

        Assert.Equal(MoveBookingOutcome.Invalid, result.Outcome);
    }
}
