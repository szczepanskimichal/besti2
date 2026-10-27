using besti2.Domain.Enums;
using besti2.Application.Bookings;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// Visits the business adds itself in the panel (phone call, walk-in)
public class ManualBookingTests
{
    // Monday 5 Jan 2099, 09:00 UTC = 10:00 in Oslo (winter time); far in the future, so the test never gets old
    private static readonly DateTime FutureMondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");

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

    private static CreateManualBookingRequest WalkIn(DateOnly date, TimeOnly time, string? email = null,
        bool sendConfirmation = false) =>
        new(HerreklippId, IngridId, date, time, "Ola Drop-in", "912 34 567", email, null, sendConfirmation);

    // check if the business can add a confirmed visit outside the opening hours (Sunday evening)
    [Fact]
    public async Task CreateByBusinessAsync_OutsideOpeningHours_IsConfirmed()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var sunday = DateOnly.FromDateTime(FutureMondayAt10).AddDays(-1);

        var result = await service.CreateByBusinessAsync(KlippOgStil, WalkIn(sunday, new TimeOnly(19, 0)));

        Assert.Equal(CreateBookingOutcome.Created, result.Outcome);
        Assert.Equal("Bekreftet", result.Booking!.Status);
    }

    // check if a visit added by the business can never overlap another visit of the same employee
    [Fact]
    public async Task CreateByBusinessAsync_OnTopOfAnotherVisit_IsSlotTaken()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        await service.CreateAsync(HerreklippHosIngrid()); // 10:00–10:30 in Oslo
        var monday = DateOnly.FromDateTime(FutureMondayAt10);

        var result = await service.CreateByBusinessAsync(KlippOgStil, WalkIn(monday, new TimeOnly(10, 15)));

        Assert.Equal(CreateBookingOutcome.SlotTaken, result.Outcome);
    }

    // check if a business cannot add a visit with another business's service
    [Fact]
    public async Task CreateByBusinessAsync_OtherBusinessService_IsNotFound()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var monday = DateOnly.FromDateTime(FutureMondayAt10);

        var result = await service.CreateByBusinessAsync(FjordBygg, WalkIn(monday, new TimeOnly(12, 0)));

        Assert.Equal(CreateBookingOutcome.NotFound, result.Outcome);
    }

    // check if the customer gets a confirmation mail only when the business asks for it
    [Fact]
    public async Task CreateByBusinessAsync_SendsMailOnlyWhenAsked()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var monday = DateOnly.FromDateTime(FutureMondayAt10);

        await service.CreateByBusinessAsync(KlippOgStil, WalkIn(monday, new TimeOnly(12, 0), "ola@test.example"));
        await service.CreateByBusinessAsync(KlippOgStil,
            WalkIn(monday, new TimeOnly(13, 0), "ola@test.example", sendConfirmation: true));

        var mail = Assert.Single(email.Sent);
        Assert.Equal("ola@test.example", mail.To);
    }

    [Fact]
    public async Task UpdateStatusAsync_VisitWithoutEmail_SendsNoMail()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var monday = DateOnly.FromDateTime(FutureMondayAt10);
        var created = await service.CreateByBusinessAsync(KlippOgStil, WalkIn(monday, new TimeOnly(12, 0), null));

        var outcome = await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Avlyst);

        Assert.Equal(UpdateBookingStatusOutcome.Updated, outcome);
        Assert.Empty(email.Sent);
    }
}