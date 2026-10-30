using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Services.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// Free times: one day for one employee (GetFreeSlotsAsync) and the day list for the month calendar (GetFreeDaysAsync)
public class AvailabilityServiceTests
{
    // Saturday 3 Jan 2099 (Klipp & Stil 10–15, Sunday closed); far in the future, so the test never gets old
    private static readonly DateOnly FutureSaturday = new(2099, 1, 3);

    private static readonly DateOnly FutureMonday = new(2099, 1, 5);

    private static readonly DateTime FutureMondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc); // 10:00 in Oslo

    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");

    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid SofieId = new("b1111111-0000-0000-0000-000000000002");

    private static readonly Guid SnekkerarbeidId = new("a2222222-0000-0000-0000-000000000003"); // Per time, 60 min
    private static readonly Guid LarsId = new("b2222222-0000-0000-0000-000000000001"); // Fjord Bygg, Monday 7–15

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

        var days = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureSaturday, 7);

        Assert.NotNull(days);
        Assert.Equal(7, days.Count);
        Assert.Equal(FutureSaturday, days[0].Date);
        Assert.NotEmpty(days[0].Slots);
        Assert.Empty(days[1].Slots); // Sunday
    }

    // check if the month calendar gets six weeks in one request, and never more
    [Fact]
    public async Task GetFreeDaysAsync_MonthCalendar_ReturnsSixWeeksAtMost()
    {
        await using var db = await CreateSeededDbAsync();

        var month = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 42);
        var tooMany = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1000);

        Assert.Equal(42, month!.Count);
        Assert.Equal(FutureMonday.AddDays(41), month[^1].Date);
        Assert.NotEmpty(month[^2].Slots); // Saturday six weeks later
        Assert.Equal(AvailabilityService.MaxDays, tooMany!.Count);
    }

    // check if "Hvem som helst" still offers 10:00 when Ingrid is busy, because Sofie is free
    [Fact]
    public async Task GetFreeDaysAsync_AnyEmployee_OffersTimeWhenOnlyOneIsBusy()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Bookings(db);
        await service.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId, FutureMondayAt10,
            "Kari Nordmann", "+4790000000", "kari@test.example", false));

        var anyone = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1);
        var onlyIngrid = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, IngridId, FutureMonday, 1);

        var tenOClock = Assert.Single(anyone![0].Slots, s => s.StartUtc == FutureMondayAt10);
        Assert.Equal(SofieId, tenOClock.EmployeeId);
        Assert.DoesNotContain(onlyIngrid![0].Slots, s => s.StartUtc == FutureMondayAt10);
    }

    // check if an unknown employee gives null (= 404), not someone else's times
    [Fact]
    public async Task GetFreeDaysAsync_UnknownEmployee_ReturnsNull()
    {
        await using var db = await CreateSeededDbAsync();

        var days = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, Guid.NewGuid(), FutureMonday, 7);

        Assert.Null(days);
    }

    // check if 3 hours of a "Per time" service are booked in a row and the last start leaves room for them
    [Fact]
    public async Task PerTimeService_ThreeHours_BooksThreeHoursInARow()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Bookings(db);

        var days = await new AvailabilityService(db).GetFreeDaysAsync(SnekkerarbeidId, LarsId, FutureMonday, 1,
            units: 3);
        var result = await service.CreateAsync(new CreateBookingRequest(SnekkerarbeidId, LarsId, FutureMondayAt10,
            "Kari Nordmann", "+4790000000", "kari@test.example", false, Units: 3));

        Assert.Equal(new DateTime(2099, 1, 5, 11, 0, 0, DateTimeKind.Utc),
            days![0].Slots[^1].StartUtc); // 12:00 in Oslo
        Assert.Equal(CreateBookingOutcome.Created, result.Outcome);
        Assert.Equal(FutureMondayAt10.AddHours(3), result.Booking!.EndUtc);
    }

    // check if more hours are refused for a fixed-price service and for more than 8 hours
    [Theory]
    [InlineData("a1111111-0000-0000-0000-000000000002", "b1111111-0000-0000-0000-000000000001", 2)] // Herreklipp
    [InlineData("a2222222-0000-0000-0000-000000000003", "b2222222-0000-0000-0000-000000000001", 9)] // Snekkerarbeid
    public async Task CreateAsync_WrongNumberOfHours_IsInvalid(string serviceId, string employeeId, int units)
    {
        await using var db = await CreateSeededDbAsync();

        var result = await Bookings(db).CreateAsync(new CreateBookingRequest(new Guid(serviceId), new Guid(employeeId),
            FutureMondayAt10, "Kari Nordmann", "+4790000000", "kari@test.example", false, Units: units));

        Assert.Equal(CreateBookingOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task GetFreeSlotsAsync_Sunday_ReturnsNoSlots_WhenClosed()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new AvailabilityService(db);
        var sunday = new DateOnly(2099, 1, 4);

        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, sunday);

        Assert.NotNull(slots);
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetFreeSlotsAsync_Saturday_UsesOpeningHoursFromDatabase()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new AvailabilityService(db);
        var saturday = new DateOnly(2099, 1, 3); // Klipp & Stil: Saturday 10-15 in Oslo (UTC+1 in winter)
        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, saturday);

        Assert.NotNull(slots);
        Assert.Equal(new DateTime(2099, 1, 3, 9, 0, 0, DateTimeKind.Utc), slots[0]); // 10:00 in Oslo
        Assert.Equal(new DateTime(2099, 1, 3, 13, 30, 0, DateTimeKind.Utc), slots[^1]); // 14:30 in Oslo
    }

    // check if an absurd date gives no slots instead of a crash
    [Fact]
    public async Task GetFreeSlotsAsync_ExtremeDate_ReturnsNoSlots()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new AvailabilityService(db);

        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, new DateOnly(9999, 12, 31));

        Assert.NotNull(slots);
        Assert.Empty(slots);
    }
}