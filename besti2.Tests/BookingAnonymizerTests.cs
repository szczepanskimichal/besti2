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

// check if more old bookings than one batch are all anonymized in a single run
    [Fact]
    public async Task AnonymizeOldBookingsAsync_MoreThanOneBatch_AnonymizesAll()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var total = BookingAnonymizer.BatchSize + 5;
        for (var i = 0; i < total; i++)
        {
            db.Bookings.Add(NewBooking(Now.AddYears(-6).AddMinutes(-i), "Gammel Kunde"));
        }
        await db.SaveChangesAsync();
        var service = new BookingAnonymizer(db);
// Act
        var count = await service.AnonymizeOldBookingsAsync(Now);
// Assert
        Assert.Equal(total, count);
        Assert.DoesNotContain(db.Bookings, b => b.CustomerName == "Gammel Kunde");
    }

// check if reviews of old visits lose the name but keep stars and text, and newer reviews stay as they are
    [Fact]
    public async Task AnonymizeOldBookingsAsync_AnonymizesReviewsOfOldVisits()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var oldBooking = NewBooking(Now.AddYears(-6), "Gammel Kunde");
        var newBooking = NewBooking(Now.AddYears(-1), "Ny Kunde");
        db.Bookings.AddRange(oldBooking, newBooking);
        db.Reviews.Add(NewReview(oldBooking, "Gammel"));
        db.Reviews.Add(NewReview(newBooking, "Ny"));
        await db.SaveChangesAsync();
        var service = new BookingAnonymizer(db);
// Act
        var count = await service.AnonymizeOldBookingsAsync(Now);
// Assert
        Assert.Equal(2, count); // one booking and one review
        var old = await db.Reviews.SingleAsync(r => r.BookingId == oldBooking.Id);
        Assert.Equal(ProfileService.FormerCustomer, old.CustomerName);
        Assert.Equal(BookingAnonymizer.AnonymizedUserId, old.CustomerUserId);
        Assert.Equal(5, old.Stars);
        Assert.Equal("Veldig fornøyd", old.Comment);
        var fresh = await db.Reviews.SingleAsync(r => r.BookingId == newBooking.Id);
        Assert.Equal("Ny", fresh.CustomerName);
        Assert.Equal("kunde-1", fresh.CustomerUserId);
    }

    private static Review NewReview(Booking booking, string name) => new()
    {
        Id = Guid.NewGuid(),
        BookingId = booking.Id,
        BusinessId = new Guid("c1111111-0000-0000-0000-000000000001"),
        CustomerUserId = "kunde-1",
        CustomerName = name,
        Stars = 5,
        Comment = "Veldig fornøyd",
        CreatedUtc = booking.EndUtc.AddDays(1)
    };
}
