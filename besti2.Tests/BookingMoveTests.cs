using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class BookingMoveTests
{
    // Monday 5 Jan 2099 (Klipp & Stil is open 09–17); far in the future, so the test never gets old
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);

    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");
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

    // A 30 min Herreklipp for Ingrid, added by the business (confirmed)
    private static async Task<Guid> VisitAsync(BookingService service, int hour, int minute = 0,
        string? email = "ola@test.example")
    {
        var result = await service.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(hour, minute), "Ola", "912 34 567", email, null, false));
        return result.Booking!.Id;
    }

    private static MoveBookingRequest To(Guid employeeId, int hour, int minute = 0, bool notify = false) =>
        new(employeeId, FutureMonday, new TimeOnly(hour, minute), notify);

    private static DateTime Oslo(int hour, int minute = 0) =>
        SlotCalculator.ToUtc(FutureMonday, new TimeOnly(hour, minute));

    // check if a visit can move to a free time with another employee and keeps its length
    [Fact]
    public async Task MoveAsync_ToFreeTime_ChangesTimeAndEmployee()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await VisitAsync(service, 10);

        var result = await service.MoveAsync(id, KlippOgStil, To(SofieId, 13));

        var booking = await db.Bookings.SingleAsync(b => b.Id == id);
        Assert.Equal(MoveBookingOutcome.Moved, result.Outcome);
        Assert.Equal(SofieId, booking.EmployeeId);
        Assert.Equal(Oslo(13), booking.StartUtc);
        Assert.Equal(Oslo(13, 30), booking.EndUtc);
    }

    // check if a visit cannot move on top of another visit, but may overlap its own old time
    [Fact]
    public async Task MoveAsync_OnlyOtherVisitsCount()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var first = await VisitAsync(service, 10);
        await VisitAsync(service, 11);

        var onOther = await service.MoveAsync(first, KlippOgStil, To(IngridId, 10, 45));
        var overItself = await service.MoveAsync(first, KlippOgStil, To(IngridId, 10, 15));

        Assert.Equal(MoveBookingOutcome.SlotTaken, onOther.Outcome);
        Assert.Equal(MoveBookingOutcome.Moved, overItself.Outcome);
    }

    // check if a visit cannot move into blocked time
    [Fact]
    public async Task MoveAsync_OnBlockedTime_IsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await VisitAsync(service, 10);
        await new TimeOffService(db).CreateAsync(KlippOgStil, new CreateTimeOffRequest(SofieId, FutureMonday,
            new TimeOnly(12, 0), FutureMonday, new TimeOnly(14, 0), false, "Lege"));

        var result = await service.MoveAsync(id, KlippOgStil, To(SofieId, 13));

        Assert.Equal(MoveBookingOutcome.Invalid, result.Outcome);
        Assert.Equal(IngridId, (await db.Bookings.SingleAsync(b => b.Id == id)).EmployeeId);
    }

    // check if cancelled and already started visits cannot be moved
    [Fact]
    public async Task MoveAsync_CancelledOrStarted_IsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var cancelled = await VisitAsync(service, 10);
        await service.UpdateStatusAsync(cancelled, KlippOgStil, BookingStatus.Avlyst);
        var started = await VisitAsync(service, 12);
        var booking = await db.Bookings.SingleAsync(b => b.Id == started);
        booking.StartUtc = DateTime.UtcNow.AddMinutes(-10);
        booking.EndUtc = DateTime.UtcNow.AddMinutes(20);
        await db.SaveChangesAsync();

        var moveCancelled = await service.MoveAsync(cancelled, KlippOgStil, To(IngridId, 14));
        var moveStarted = await service.MoveAsync(started, KlippOgStil, To(IngridId, 15));

        Assert.Equal(MoveBookingOutcome.Invalid, moveCancelled.Outcome);
        Assert.Equal(MoveBookingOutcome.Invalid, moveStarted.Outcome);
    }

    // check if another business cannot move the visit
    [Fact]
    public async Task MoveAsync_OtherBusiness_IsNotFound()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await VisitAsync(service, 10);

        var result = await service.MoveAsync(id, FjordBygg, To(IngridId, 13));

        Assert.Equal(MoveBookingOutcome.NotFound, result.Outcome);
    }

    // check if the customer gets the new time by mail only when asked and when there is an address
    [Fact]
    public async Task MoveAsync_SendsMailOnlyWhenAsked()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var quiet = await VisitAsync(service, 9);
        var noAddress = await VisitAsync(service, 10, email: null);
        var told = await VisitAsync(service, 11);

        await service.MoveAsync(quiet, KlippOgStil, To(IngridId, 13));
        await service.MoveAsync(noAddress, KlippOgStil, To(IngridId, 14, notify: true));
        await service.MoveAsync(told, KlippOgStil, To(IngridId, 15, notify: true));

        var mail = Assert.Single(email.Sent);
        Assert.Equal("ola@test.example", mail.To);
        Assert.Contains("Ny tid: mandag 5. januar kl. 15:00", mail.Body);
        Assert.Contains("Tidligere tid: mandag 5. januar kl. 11:00", mail.Body);
    }
}