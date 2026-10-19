using besti2.Domain.Enums;
using besti2.Application.Bookings;
using besti2.Application.Businesses;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;


namespace besti2.Tests;

public class BookingServiceTests
{
    // Monday 7 Jan 2030, 09:00 UTC = 10:00 in Oslo (winter time)
    private static readonly DateTime FutureMondayAt10 = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);

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

    [Fact]
    public async Task CreateAsync_SameSlotTwice_SecondIsSlotTaken()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
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
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var result = await service.CreateAsync(HerreklippHosIngrid(email: "abc"));

        Assert.Equal(CreateBookingOutcome.Invalid, result.Outcome);
        Assert.Equal(0, await db.Bookings.CountAsync());
    }


    [Fact]
    public async Task CreateAsync_SendsConfirmationEmailWithOsloTime()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);

        await service.CreateAsync(HerreklippHosIngrid());

        var sent = Assert.Single(email.Sent);
        Assert.Equal("kari@test.example", sent.To);
        Assert.Contains("Herreklipp", sent.Body);
        Assert.Contains("mandag 7. januar kl. 10:00", sent.Body);
    }

    [Fact]
    public async Task UpdateStatusAsync_Confirm_SendsStatusEmail()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid());

        await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Bekreftet);

        Assert.Equal(2, email.Sent.Count);
        Assert.Contains("bekreftet", email.Sent[^1].Subject);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_DoesNotSendEmail()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid());
        await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Avlyst);

        await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Bekreftet);

        Assert.Equal(2, email.Sent.Count);
    }

    [Fact]
    public async Task GetForBusinessAsync_ReturnsOnlyOwnBusinessBookings()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        await service.CreateAsync(HerreklippHosIngrid());

        var own = await service.GetForBusinessAsync(KlippOgStil);
        var other = await service.GetForBusinessAsync(FjordBygg);

        Assert.Single(own);
        Assert.Equal("Ingrid Hansen", own[0].EmployeeName);
        Assert.Empty(other);
    }

    [Fact]
    public async Task UpdateStatusAsync_OtherBusiness_ReturnsNotFound()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid());

        var outcome = await service.UpdateStatusAsync(created.Booking!.Id, FjordBygg, BookingStatus.Bekreftet);

        Assert.Equal(UpdateBookingStatusOutcome.NotFound, outcome);
        Assert.Equal(BookingStatus.Venter, (await db.Bookings.SingleAsync()).Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelledBooking_CannotBeConfirmed()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid());

        var cancel = await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Avlyst);
        var confirm = await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Bekreftet);

        Assert.Equal(UpdateBookingStatusOutcome.Updated, cancel);
        Assert.Equal(UpdateBookingStatusOutcome.InvalidTransition, confirm);
    }

    [Fact]
    public async Task GetFreeSlotsAsync_Sunday_ReturnsNoSlots_WhenClosed()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(),
            NullLogger<BookingService>.Instance);
        var sunday = new DateOnly(2030, 1, 6);


        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, sunday);

        Assert.NotNull(slots);
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetFreeSlotsAsync_Saturday_UsesOpeningHoursFromDatabase()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var saturday = new DateOnly(2030, 1, 5); // Klipp & Stil: Saturday 10-15 in Oslo (UTC+1 in winter)

        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, saturday);

        Assert.NotNull(slots);
        Assert.Equal(new DateTime(2030, 1, 5, 9, 0, 0, DateTimeKind.Utc), slots[0]); // 10:00 in Oslo
        Assert.Equal(new DateTime(2030, 1, 5, 13, 30, 0, DateTimeKind.Utc), slots[^1]); // 14:30 in Oslo
    }

    // check if GetForBusinessAsync hides bookings that have already ended
    [Fact]
    public async Task GetForBusinessAsync_HidesPastBookings()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(-1),
            EndUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            CustomerName = "Gammel Kunde",
            CustomerPhone = "+4790000001",
            CustomerEmail = "gammel@test.example",
            Status = BookingStatus.Bekreftet,
            ServiceId = HerreklippId,
            EmployeeId = IngridId
        });
        await db.SaveChangesAsync();
        await service.CreateAsync(HerreklippHosIngrid());

        var result = await service.GetForBusinessAsync(KlippOgStil);

        var booking = Assert.Single(result);
        Assert.Equal("Kari Nordmann", booking.CustomerName);
    }
}