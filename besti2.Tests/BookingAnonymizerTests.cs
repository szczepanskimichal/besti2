using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

public class BookingAnonymizerTests
{
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);

    private static Booking NewBooking(DateTime startUtc, string name) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CustomerName = name,
        CustomerPhone = "+4790000000",
        CustomerEmail = "kunde@test.example",
        MarketingConsent = true,
        MarketingConsentUtc = startUtc,
        CustomerUserId = "kunde-1",
        Status = BookingStatus.Bekreftet,
        ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
        EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001")
    };

// check if only bookings older than 5 years lose their customer data
    [Fact]
    public async Task AnonymizeOldBookingsAsync_AnonymizesOnlyOldBookings()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Bookings.Add(NewBooking(Now.AddYears(-6), "Gammel Kunde"));
        db.Bookings.Add(NewBooking(Now.AddYears(-1), "Ny Kunde"));
        await db.SaveChangesAsync();
        var service = new BookingAnonymizer(db);
// Act
        var count = await service.AnonymizeOldBookingsAsync(Now);
// Assert
        Assert.Equal(1, count);
        var old = await db.Bookings.SingleAsync(b => b.CustomerEmail == BookingAnonymizer.AnonymizedEmail);
        Assert.Equal("Anonymisert", old.CustomerName);
        Assert.False(old.MarketingConsent);
        Assert.Null(old.CustomerUserId);
        Assert.Contains(db.Bookings, b => b.CustomerName == "Ny Kunde");
    }

// check if running twice does not touch already anonymized bookings again
    [Fact]
    public async Task AnonymizeOldBookingsAsync_SecondRunFindsNothing()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Bookings.Add(NewBooking(Now.AddYears(-6), "Gammel Kunde"));
        await db.SaveChangesAsync();
        var service = new BookingAnonymizer(db);
        await service.AnonymizeOldBookingsAsync(Now);
// Act
        var count = await service.AnonymizeOldBookingsAsync(Now);
// Assert
        Assert.Equal(0, count);
    }
}