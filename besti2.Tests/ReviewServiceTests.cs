using besti2.Application.Reviews;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

public class ReviewServiceTests
{
    private static readonly Guid KlippId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordId = new("22222222-2222-2222-2222-222222222222");

    private static async Task<(ReviewService Service, AppDbContext Db)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (new ReviewService(db), db);
    }

// A booking at Klipp & Stil for customer "kunde-1", starting hoursFromNow from now

    private static async Task<Booking> AddBookingAsync(AppDbContext db, double hoursFromNow,
        BookingStatus status = BookingStatus.Bekreftet)
    {
        var start = DateTime.UtcNow.AddHours(hoursFromNow);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = start,
            EndUtc = start.AddMinutes(30),
            CustomerName = "Ola Nordmann",
            CustomerPhone = "12345678",
            CustomerEmail = "ola@example.example",
            Status = status,
            CustomerUserId = "kunde-1",
            EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001"),
            ServiceId = new Guid("a1111111-0000-0000-0000-000000000002")
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

// check if a review after the visit is saved with the first name only
    [Fact]
    public async Task CreateAsync_AfterVisit_SavesReview()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: -24);
        var error = await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 5, " Supert! "));
        Assert.Null(error);
        var review = Assert.Single(db.Reviews);
        Assert.Equal("Ola", review.CustomerName);
        Assert.Equal("Supert!", review.Comment);
        Assert.Equal(KlippId, review.BusinessId);
    }

// check if a review before the visit is refused
    [Fact]
    public async Task CreateAsync_BeforeVisit_Fails()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: 24);
        var error = await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 5, null));
        Assert.Equal("Du kan gi en vurdering etter timen.", error);
    }

// check if a cancelled booking cannot be reviewed
    [Fact]
    public async Task CreateAsync_CancelledBooking_Fails()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: -24, BookingStatus.Avlyst);
        var error = await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 4, null));
        Assert.Equal("Du kan gi en vurdering etter timen.", error);
    }

// check if the same booking cannot be reviewed twice
    [Fact]
    public async Task CreateAsync_Twice_Fails()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: -24);
        await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 5, null));
        var error = await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 1, null));
        Assert.Equal("Du har allerede vurdert denne timen.", error);
    }

// check if another customer cannot review someone else's booking
    [Fact]
    public async Task CreateAsync_OtherCustomersBooking_Fails()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: -24);
        var error = await service.CreateAsync("kunde-2", new CreateReviewRequest(booking.Id, 1, null));
        Assert.Equal("Fant ikke bestillingen.", error);
        Assert.Empty(db.Reviews);
    }

// check if stars outside 1-5 are refused
    [Fact]
    public async Task CreateAsync_InvalidStars_Fails()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: -24);
        var error = await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 6, null));
        Assert.Equal("Velg mellom 1 og 5 stjerner.", error);
    }

// check if the average is calculated
    [Fact]
    public async Task GetForBusinessAsync_ReturnsAverage()
    {
        var (service, db) = await CreateAsync();
        var first = await AddBookingAsync(db, hoursFromNow: -48);
        var second = await AddBookingAsync(db, hoursFromNow: -24);
        await service.CreateAsync("kunde-1", new CreateReviewRequest(first.Id, 5, null));
        await service.CreateAsync("kunde-1", new CreateReviewRequest(second.Id, 4, null));
        var result = await service.GetForBusinessAsync(KlippId);
        Assert.Equal(2, result.Count);
        Assert.Equal(4.5, result.Average);
    }

// check if only the reviewed business can reply
    [Fact]
    public async Task ReplyAsync_OnlyOwnBusiness()
    {
        var (service, db) = await CreateAsync();
        var booking = await AddBookingAsync(db, hoursFromNow: -24);
        await service.CreateAsync("kunde-1", new CreateReviewRequest(booking.Id, 3, null));
        var review = db.Reviews.Single();
        var otherError = await service.ReplyAsync(FjordId, review.Id, "Takk!");
        var ownError = await service.ReplyAsync(KlippId, review.Id, "Takk for besøket!");
        Assert.Equal("Fant ikke vurderingen.", otherError);
        Assert.Null(ownError);
        Assert.Equal("Takk for besøket!", db.Reviews.Single().BusinessReply);
    }
}