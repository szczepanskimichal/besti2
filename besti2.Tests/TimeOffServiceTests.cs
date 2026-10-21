using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class TimeOffServiceTests
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

    private static BookingService Bookings(AppDbContext db) =>
        new(db, new FakeEmailService(), NullLogger<BookingService>.Instance);

    private static CreateTimeOffRequest Block(Guid? employeeId, int fromHour, int toHour) =>
        new(employeeId, FutureMonday, new TimeOnly(fromHour, 0), FutureMonday, new TimeOnly(toHour, 0), false, "Lege");

    private static DateTime Oslo(int hour) => SlotCalculator.ToUtc(FutureMonday, new TimeOnly(hour, 0));

    // check if blocked time of one employee removes only those free times, and only for that employee
    [Fact]
    public async Task CreateAsync_EmployeeBlock_RemovesThoseFreeTimes()
    {
        await using var db = await CreateSeededDbAsync();
        var timeOff = new TimeOffService(db);

        await timeOff.CreateAsync(KlippOgStil, Block(IngridId, 10, 12));
        var ingrid = await Bookings(db).GetFreeSlotsAsync(HerreklippId, IngridId, FutureMonday);
        var sofie = await Bookings(db).GetFreeSlotsAsync(HerreklippId, SofieId, FutureMonday);

        Assert.Contains(Oslo(9), ingrid!);
        Assert.DoesNotContain(Oslo(10), ingrid!);
        Assert.DoesNotContain(Oslo(11), ingrid!);
        Assert.Contains(Oslo(12), ingrid!);
        Assert.Contains(Oslo(10), sofie!);
    }

    // check if a closed day for the whole business leaves no free time and hides it from the search by date
    [Fact]
    public async Task CreateAsync_WholeBusinessAllDay_NoFreeTimeForAnyone()
    {
        await using var db = await CreateSeededDbAsync();
        var timeOff = new TimeOffService(db);
        var closed = new CreateTimeOffRequest(null, FutureMonday, default, FutureMonday, default, true, "Stengt");

        await timeOff.CreateAsync(KlippOgStil, closed);
        var ingrid = await Bookings(db).GetFreeSlotsAsync(HerreklippId, IngridId, FutureMonday);
        var sofie = await Bookings(db).GetFreeSlotsAsync(HerreklippId, SofieId, FutureMonday);
        var search = await new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage())
            .GetAllAsync(date: FutureMonday);

        Assert.Empty(ingrid!);
        Assert.Empty(sofie!);
        Assert.DoesNotContain(search, b => b.Id == KlippOgStil);
        Assert.Contains(search, b => b.Id == FjordBygg);
    }

    // check if neither the customer nor the business itself can book blocked time
    [Fact]
    public async Task Booking_OnBlockedTime_IsRefused()
    {
        await using var db = await CreateSeededDbAsync();
        await new TimeOffService(db).CreateAsync(KlippOgStil, Block(IngridId, 10, 12));
        var bookings = Bookings(db);

        var online = await bookings.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId, Oslo(10),
            "Kari Nordmann", "+4790000000", "kari@test.example", false));
        var manual = await bookings.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(11, 0), "Ola", "912 34 567", null, null, false));

        Assert.Equal(CreateBookingOutcome.SlotTaken, online.Outcome);
        Assert.Equal(CreateBookingOutcome.Invalid, manual.Outcome);
        Assert.Equal(0, await db.Bookings.CountAsync());
    }

    // check if visits already in the blocked time are counted for the warning, but not cancelled
    [Fact]
    public async Task CreateAsync_OverExistingVisit_CountsItAndKeepsIt()
    {
        await using var db = await CreateSeededDbAsync();
        await Bookings(db).CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(10, 0), "Ola", "912 34 567", null, null, false));

        var result = await new TimeOffService(db).CreateAsync(KlippOgStil, Block(null, 9, 17));

        Assert.Equal(1, result!.OverlappingBookings);
        Assert.Equal(1, await db.Bookings.CountAsync(b => b.Status == Domain.Enums.BookingStatus.Bekreftet));
    }

    // check if the end must be after the start, and another business's employee is not accepted
    [Fact]
    public async Task CreateAsync_WrongInput_IsRefused()
    {
        await using var db = await CreateSeededDbAsync();
        var timeOff = new TimeOffService(db);

        var backwards = await timeOff.CreateAsync(KlippOgStil, Block(IngridId, 12, 10));
        var otherBusiness = await timeOff.CreateAsync(FjordBygg, Block(IngridId, 10, 12));

        Assert.NotNull(backwards!.Error);
        Assert.Null(otherBusiness);
        Assert.Equal(0, await db.TimeOffs.CountAsync());
    }

    // check if a business can delete only its own blocked time
    [Fact]
    public async Task DeleteAsync_OnlyOwnBusiness()
    {
        await using var db = await CreateSeededDbAsync();
        var timeOff = new TimeOffService(db);
        var created = await timeOff.CreateAsync(KlippOgStil, Block(IngridId, 10, 12));

        var byOther = await timeOff.DeleteAsync(FjordBygg, created!.TimeOff!.Id);
        var byOwner = await timeOff.DeleteAsync(KlippOgStil, created.TimeOff.Id);

        Assert.False(byOther);
        Assert.True(byOwner);
        Assert.Equal(0, await db.TimeOffs.CountAsync());
    }

    // check if a holiday that started before the shown week is still in the calendar
    [Fact]
    public async Task GetForBusinessAsync_IncludesBlockStartedEarlier()
    {
        await using var db = await CreateSeededDbAsync();
        var timeOff = new TimeOffService(db);
        var holiday = new CreateTimeOffRequest(IngridId, FutureMonday.AddDays(-10), default, FutureMonday.AddDays(2),
            default, true, "Ferie");
        await timeOff.CreateAsync(KlippOgStil, holiday);

        var week = await timeOff.GetForBusinessAsync(KlippOgStil, FutureMonday, FutureMonday.AddDays(6));

        var item = Assert.Single(week);
        Assert.Equal("Ingrid Hansen", item.EmployeeName);
    }
}
