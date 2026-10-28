using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Services.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// BESTI sammen: a time is free for 2 people only when 2 different employees are free at that time
public class BestiSammenAvailabilityTests
{
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);
    private static readonly DateTime FutureMondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc); // 10:00 in Oslo

    private static readonly Guid KlippOgStilId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid SofieId = new("b1111111-0000-0000-0000-000000000002");

    private static readonly Guid SnekkerarbeidId = new("a2222222-0000-0000-0000-000000000003"); // Fjord Bygg: only Lars

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Task BookAsync(AppDbContext db, Guid employeeId, DateTime startUtc) =>
        new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance)
            .CreateAsync(new CreateBookingRequest(HerreklippId, employeeId, startUtc,
                "Kari Nordmann", "+4790000000", "kari@test.example", false));

    // check if only the times when both employees are free come out, with both of them
    [Fact]
    public void FreeForPersons_TwoPeople_OnlySharedTimes()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var nine = new DateTime(2099, 1, 5, 8, 0, 0, DateTimeKind.Utc);
        var ten = nine.AddHours(1);
        var eleven = nine.AddHours(2);

        var shared = SlotCalculator.FreeForPersons([(a, [nine, ten]), (b, [ten, eleven])], 2);

        var only = Assert.Single(shared);
        Assert.Equal(ten, only.StartUtc);
        Assert.Equal([a, b], only.EmployeeIds);
    }

    // check if one person alone still gets every time of every employee (same as "Hvem som helst")
    [Fact]
    public void FreeForPersons_OnePerson_EveryTime()
    {
        var a = Guid.NewGuid();
        var nine = new DateTime(2099, 1, 5, 8, 0, 0, DateTimeKind.Utc);

        var times = SlotCalculator.FreeForPersons([(a, [nine, nine.AddHours(1)]), (Guid.NewGuid(), [])], 1);

        Assert.Equal(2, times.Count);
    }

    // check if a business with one employee has no times for 2 people
    [Fact]
    public async Task GetFreeDaysAsync_OneEmployee_NoTimesForTwo()
    {
        await using var db = await CreateSeededDbAsync();

        var days = await new AvailabilityService(db).GetFreeDaysAsync(SnekkerarbeidId, null, FutureMonday, 1,
            persons: 2);

        Assert.NotNull(days);
        Assert.Empty(days[0].Slots);
    }

    // check if 10:00 disappears for 2 people when Ingrid is busy, but stays for one person (Sofie is free)
    [Fact]
    public async Task GetFreeDaysAsync_OneEmployeeBusy_TimeGoneForTwo()
    {
        await using var db = await CreateSeededDbAsync();
        await BookAsync(db, IngridId, FutureMondayAt10);

        var alone = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1);
        var together = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1,
            persons: 2);

        Assert.Contains(alone![0].Slots, s => s.StartUtc == FutureMondayAt10);
        Assert.DoesNotContain(together![0].Slots, s => s.StartUtc == FutureMondayAt10);
        Assert.Contains(together[0].Slots, s => s.StartUtc == FutureMondayAt10.AddHours(1));
    }

    // check if a day off for one of the two employees leaves no times for 2 people that day
    [Fact]
    public async Task GetFreeDaysAsync_TimeOffForOne_NoTimesForTwo()
    {
        await using var db = await CreateSeededDbAsync();
        db.TimeOffs.Add(new TimeOff
        {
            Id = Guid.NewGuid(), BusinessId = KlippOgStilId, EmployeeId = SofieId,
            StartUtc = FutureMondayAt10.AddHours(-10), EndUtc = FutureMondayAt10.AddHours(14)
        });
        await db.SaveChangesAsync();

        var together = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1,
            persons: 2);

        Assert.Empty(together![0].Slots);
    }

    // check if the inviter's chosen employee keeps their place and the friend gets the other one
    [Fact]
    public async Task GetFreeEmployeesAsync_ChosenEmployeeFirst()
    {
        await using var db = await CreateSeededDbAsync();

        var employees = await new AvailabilityService(db).GetFreeEmployeesAsync(HerreklippId, SofieId,
            FutureMondayAt10, 1, 2);

        Assert.Equal([SofieId, IngridId], employees);
    }

    // check if a busy time gives an empty list (the invitation cannot be sent or accepted)
    [Fact]
    public async Task GetFreeEmployeesAsync_NotFreeForTwo_Empty()
    {
        await using var db = await CreateSeededDbAsync();
        await BookAsync(db, SofieId, FutureMondayAt10);

        var employees = await new AvailabilityService(db).GetFreeEmployeesAsync(HerreklippId, null,
            FutureMondayAt10, 1, 2);

        Assert.NotNull(employees);
        Assert.Empty(employees);
    }

    // check if more people than BESTI sammen allows gives null (= 404), not a wrong answer
    [Fact]
    public async Task GetFreeDaysAsync_TooManyPersons_ReturnsNull()
    {
        await using var db = await CreateSeededDbAsync();

        var days = await new AvailabilityService(db).GetFreeDaysAsync(HerreklippId, null, FutureMonday, 1,
            persons: 9);

        Assert.Null(days);
    }
}
