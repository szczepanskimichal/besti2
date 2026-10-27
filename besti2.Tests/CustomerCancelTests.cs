using besti2.Domain.Enums;
using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// The customer cancels a visit: own visits only, before the start and before the free-cancel deadline
public class CustomerCancelTests
{
    // Monday 5 Jan 2099, 09:00 UTC = 10:00 in Oslo (winter time); far in the future, so the test never gets old
    private static readonly DateTime FutureMondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
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

    // A confirmed visit for kunde-1 at Klipp & Stil that starts in the given number of hours
    private static async Task<Booking> AddConfirmedBookingAsync(AppDbContext db, double startsInHours)
    {
        var start = DateTime.UtcNow.AddHours(startsInHours);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = start,
            EndUtc = start.AddMinutes(30),
            CustomerName = "Kari Nordmann",
            CustomerPhone = "+4790000000",
            CustomerEmail = "kari@test.example",
            Status = BookingStatus.Bekreftet,
            CustomerUserId = "kunde-1",
            ServiceId = HerreklippId,
            EmployeeId = IngridId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    // check if a customer can cancel their own upcoming booking
    [Fact]
    public async Task CancelByCustomerAsync_OwnFutureBooking_IsCancelled()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            EmailConfirmed = true, BusinessId = KlippOgStil
        });
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid(), "kunde-1");
        var outcome = await service.CancelByCustomerAsync(created.Booking!.Id, "kunde-1");
        Assert.Equal(UpdateBookingStatusOutcome.Updated, outcome);
        Assert.Equal(BookingStatus.Avlyst, (await db.Bookings.SingleAsync()).Status);
        // 1st mail = "Bestilling mottatt" to the customer, 2nd = cancellation to the business
        Assert.Equal(2, email.Sent.Count);
        Assert.StartsWith("Avbestilling", email.Sent[^1].Subject);
        Assert.Equal("eier@klippogstil.example", email.Sent[^1].To);
    }

    // check if the customer's reason reaches the business in the e-mail and the bell
    [Fact]
    public async Task CancelByCustomerAsync_WithReason_BusinessGetsReason()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            EmailConfirmed = true, BusinessId = KlippOgStil
        });
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid(), "kunde-1");

        await service.CancelByCustomerAsync(created.Booking!.Id, "kunde-1", "  Jeg er syk.  ");

        Assert.Contains("Melding fra kunden: Jeg er syk.\n", email.Sent[^1].Body);
        var bell = await db.Notifications.SingleAsync(n => n.BusinessId == KlippOgStil && n.Text.Contains("avbestilt"));
        Assert.EndsWith(": Jeg er syk.", bell.Text);
    }

    // check if nobody can cancel someone else's booking
    [Fact]
    public async Task CancelByCustomerAsync_OtherCustomer_ReturnsNotFound()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid(), "kunde-1");
        var outcome = await service.CancelByCustomerAsync(created.Booking!.Id, "kunde-2");
        Assert.Equal(UpdateBookingStatusOutcome.NotFound, outcome);
        Assert.Equal(BookingStatus.Venter, (await db.Bookings.SingleAsync()).Status);
    }

    // check if a booking that already started cannot be cancelled
    [Fact]
    public async Task CancelByCustomerAsync_PastBooking_ReturnsInvalidTransition()

    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(-1),
            EndUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            CustomerName = "Kari Nordmann",
            CustomerPhone = "+4790000000",
            CustomerEmail = "kari@test.example",
            Status = BookingStatus.Bekreftet,
            CustomerUserId = "kunde-1",
            ServiceId = HerreklippId,
            EmployeeId = IngridId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        var outcome = await service.CancelByCustomerAsync(booking.Id, "kunde-1");
        Assert.Equal(UpdateBookingStatusOutcome.InvalidTransition, outcome);
    }

    // check if a confirmed visit cannot be cancelled online after the deadline (default 24 hours)
    [Fact]
    public async Task CancelByCustomerAsync_ConfirmedAfterDeadline_ReturnsTooLate()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var booking = await AddConfirmedBookingAsync(db, 3);

        var outcome = await service.CancelByCustomerAsync(booking.Id, "kunde-1");

        Assert.Equal(UpdateBookingStatusOutcome.TooLate, outcome);
        Assert.Equal(BookingStatus.Bekreftet, (await db.Bookings.SingleAsync()).Status);
    }

    // check if a confirmed visit can be cancelled before the deadline
    [Fact]
    public async Task CancelByCustomerAsync_ConfirmedBeforeDeadline_IsCancelled()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var booking = await AddConfirmedBookingAsync(db, 30);

        var outcome = await service.CancelByCustomerAsync(booking.Id, "kunde-1");

        Assert.Equal(UpdateBookingStatusOutcome.Updated, outcome);
    }

    // check if "Når som helst" (0 hours) lets the customer cancel until the visit starts
    [Fact]
    public async Task CancelByCustomerAsync_NoDeadline_IsCancelled()
    {
        await using var db = await CreateSeededDbAsync();
        (await db.Businesses.SingleAsync(b => b.Id == KlippOgStil)).FreeCancelHours = 0;
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var booking = await AddConfirmedBookingAsync(db, 1);

        var outcome = await service.CancelByCustomerAsync(booking.Id, "kunde-1");

        Assert.Equal(UpdateBookingStatusOutcome.Updated, outcome);
    }

    // check if deleting the account still cancels a visit after the deadline
    [Fact]
    public async Task CancelByCustomerAsync_IgnoreDeadline_IsCancelled()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var booking = await AddConfirmedBookingAsync(db, 3);

        var outcome = await service.CancelByCustomerAsync(booking.Id, "kunde-1", ignoreDeadline: true);

        Assert.Equal(UpdateBookingStatusOutcome.Updated, outcome);
    }
}