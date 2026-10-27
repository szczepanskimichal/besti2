using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class BookingNoShowTests
{
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);
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

    // A visit for Ola; hoursAgo moves it into the past (the start time is what matters)
    private static async Task<Guid> VisitAsync(AppDbContext db, BookingService service, int hour, int? hoursAgo = null)
    {
        var result = await service.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(hour, 0), "Ola", "912 34 567", "ola@test.example", null, false));
        var id = result.Booking!.Id;
        if (hoursAgo is int ago)
        {
            var booking = await db.Bookings.SingleAsync(b => b.Id == id);
            booking.StartUtc = DateTime.UtcNow.AddHours(-ago);
            booking.EndUtc = booking.StartUtc.AddMinutes(30);
            await db.SaveChangesAsync();
        }
        return id;
    }

    // check if "Møtte ikke" is not possible before the visit starts
    [Fact]
    public async Task NoShow_BeforeStart_IsRejected()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await VisitAsync(db, service, 10);

        var outcome = await service.UpdateStatusAsync(id, KlippOgStil, BookingStatus.MotteIkke);

        Assert.Equal(UpdateBookingStatusOutcome.InvalidTransition, outcome);
    }

    // check if a started visit can be marked "Møtte ikke" and back to "Møtte", without any e-mail to the customer
    [Fact]
    public async Task NoShow_AfterStart_CanBeSetAndUndone_WithoutEmail()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var id = await VisitAsync(db, service, 10, hoursAgo: 2);

        Assert.Equal(UpdateBookingStatusOutcome.Updated,
            await service.UpdateStatusAsync(id, KlippOgStil, BookingStatus.MotteIkke));
        Assert.Equal(BookingStatus.MotteIkke, (await db.Bookings.SingleAsync(b => b.Id == id)).Status);
        Assert.Equal(UpdateBookingStatusOutcome.Updated,
            await service.UpdateStatusAsync(id, KlippOgStil, BookingStatus.Bekreftet));
        Assert.Equal(BookingStatus.Bekreftet, (await db.Bookings.SingleAsync(b => b.Id == id)).Status);
        Assert.Empty(email.Sent);
    }

    // check if the panel shows how many times this customer did not come to this business
    [Fact]
    public async Task PanelBookings_CountNoShowsForSameCustomer()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var first = await VisitAsync(db, service, 10, hoursAgo: 48);
        var second = await VisitAsync(db, service, 11, hoursAgo: 24);
        await VisitAsync(db, service, 12);
        await service.UpdateStatusAsync(first, KlippOgStil, BookingStatus.MotteIkke);
        await service.UpdateStatusAsync(second, KlippOgStil, BookingStatus.MotteIkke);

        var bookings = await service.GetForBusinessAsync(KlippOgStil, new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1));

        Assert.Equal(3, bookings.Count);
        Assert.All(bookings, b => Assert.Equal(2, b.NoShowCount));
    }
}
