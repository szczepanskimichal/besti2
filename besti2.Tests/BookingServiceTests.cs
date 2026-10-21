using besti2.Domain.Enums;
using besti2.Application.Bookings;
using besti2.Application.Businesses;
using besti2.Application.Reviews;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;


namespace besti2.Tests;

public class BookingServiceTests
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
        Assert.Contains("mandag 5. januar kl. 10:00", sent.Body);
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
        var sunday = new DateOnly(2099, 1, 4);


        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, sunday);

        Assert.NotNull(slots);
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetFreeSlotsAsync_Saturday_UsesOpeningHoursFromDatabase()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var saturday = new DateOnly(2099, 1, 3); // Klipp & Stil: Saturday 10-15 in Oslo (UTC+1 in winter)
        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, saturday);

        Assert.NotNull(slots);
        Assert.Equal(new DateTime(2099, 1, 3, 9, 0, 0, DateTimeKind.Utc), slots[0]); // 10:00 in Oslo
        Assert.Equal(new DateTime(2099, 1, 3, 13, 30, 0, DateTimeKind.Utc), slots[^1]); // 14:30 in Oslo
    }
    
    // check if the panel shows yesterday's visit, but not visits older than 14 days
    [Fact]
    public async Task GetForBusinessAsync_ShowsTheLast14DaysOnly()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(-20),
            EndUtc = DateTime.UtcNow.AddDays(-20).AddMinutes(30),
            CustomerName = "Gammel Kunde",
            CustomerPhone = "+4790000001",
            CustomerEmail = "gammel@test.example",
            Status = BookingStatus.Bekreftet,
            ServiceId = HerreklippId,
            EmployeeId = IngridId
        });
        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(-1),
            EndUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            CustomerName = "I Går",
            CustomerPhone = "+4790000002",
            CustomerEmail = "igaar@test.example",
            Status = BookingStatus.Venter,
            ServiceId = HerreklippId,
            EmployeeId = IngridId
        });
        await db.SaveChangesAsync();
        await service.CreateAsync(HerreklippHosIngrid());

        var result = await service.GetForBusinessAsync(KlippOgStil);

        Assert.Equal(["I Går", "Kari Nordmann"], result.Select(b => b.CustomerName));
    }

    [Fact]
    public async Task CreateAsync_SavesNoteForTheBusiness()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var request = HerreklippHosIngrid() with { Note = "  Jeg har langt hår  " };
        await service.CreateAsync(request);
        var booking = Assert.Single(await service.GetForBusinessAsync(KlippOgStil));
        Assert.Equal("Jeg har langt hår", booking.CustomerNote);
    }

    // check if a booking made from an account remembers which customer made it
    [Fact]
    public async Task CreateAsync_WithAccount_SavesCustomerUserId()

    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        await service.CreateAsync(HerreklippHosIngrid(), "kunde-123");
        var booking = Assert.Single(db.Bookings);
        Assert.Equal("kunde-123", booking.CustomerUserId);
    }

    // check if a customer sees only their own bookings
    [Fact]
    public async Task GetForCustomerAsync_ReturnsOnlyOwnBookings()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        await service.CreateAsync(HerreklippHosIngrid(), "kunde-1");
        var own = await service.GetForCustomerAsync("kunde-1");
        var other = await service.GetForCustomerAsync("kunde-2");
        var booking = Assert.Single(own);
        Assert.Equal("Herreklipp", booking.ServiceName);
        Assert.Empty(other);
    }

    // check if a customer can cancel their own upcoming booking
    [Fact]
    public async Task CancelByCustomerAsync_OwnFutureBooking_IsCancelled()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var created = await service.CreateAsync(HerreklippHosIngrid(), "kunde-1");
        var outcome = await service.CancelByCustomerAsync(created.Booking!.Id, "kunde-1");
        Assert.Equal(UpdateBookingStatusOutcome.Updated, outcome);
        Assert.Equal(BookingStatus.Avlyst, (await db.Bookings.SingleAsync()).Status);
        // 1st mail = "Bestilling mottatt" to the customer, 2nd = cancellation to the business
        Assert.Equal(2, email.Sent.Count);
        Assert.StartsWith("Avbestilling", email.Sent[^1].Subject);
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
    
    // check if a phone number with letters is refused
    [Fact]
    public async Task CreateAsync_InvalidPhone_ReturnsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);

        var result = await service.CreateAsync(HerreklippHosIngrid() with { CustomerPhone = "aaa" });

        Assert.Equal(CreateBookingOutcome.Invalid, result.Outcome);
    }
    
    // check if a longer service cannot overlap a booking that starts inside it
    [Fact]
    public async Task CreateAsync_OverlappingLongerService_IsSlotTaken()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        // Herreklipp (30 min) at 10:30, then Dameklipp (60 min) at 10:00 would overlap 10:30–11:00
        var first = await service.CreateAsync(HerreklippHosIngrid() with { StartUtc = FutureMondayAt10.AddMinutes(30) });
        var dameklipp = HerreklippHosIngrid() with { ServiceId = new Guid("a1111111-0000-0000-0000-000000000001") };
        var second = await service.CreateAsync(dameklipp);

        Assert.Equal(CreateBookingOutcome.Created, first.Outcome);
        Assert.Equal(CreateBookingOutcome.SlotTaken, second.Outcome);
    }

    // check if a cancelled booking frees the time for someone else
    [Fact]
    public async Task CreateAsync_AfterCancellation_SlotIsFreeAgain()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var first = await service.CreateAsync(HerreklippHosIngrid());
        await service.UpdateStatusAsync(first.Booking!.Id, KlippOgStil, BookingStatus.Avlyst);

        var second = await service.CreateAsync(HerreklippHosIngrid(email: "ola@test.example"));

        Assert.Equal(CreateBookingOutcome.Created, second.Outcome);
    }

    // check if 00:30 on Monday in Oslo (Sunday 23:30 UTC) uses Monday's opening hours
    [Fact]
    public async Task CreateAsync_JustAfterMidnightInOslo_UsesTheOsloDay()
    {
        await using var db = await CreateSeededDbAsync();
        var monday = db.OpeningHours.Single(h => h.BusinessId == KlippOgStil && h.DayOfWeek == DayOfWeek.Monday);
        monday.OpensAt = new TimeOnly(0, 0);
        await db.SaveChangesAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        // Monday 5 Jan 2099 00:30 Oslo = Sunday 4 Jan 23:30 UTC
        var request = HerreklippHosIngrid() with { StartUtc = new DateTime(2099, 1, 4, 23, 30, 0, DateTimeKind.Utc) };

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingOutcome.Created, result.Outcome);
    }

    // check if a business cannot cancel a visit that has already started
    [Fact]
    public async Task UpdateStatusAsync_CancelAfterStart_IsInvalidTransition()
    {
        await using var db = await CreateSeededDbAsync();
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddHours(-2),
            EndUtc = DateTime.UtcNow.AddHours(-1),
            CustomerName = "Kari Nordmann",
            CustomerPhone = "+4790000000",
            CustomerEmail = "kari@test.example",
            Status = BookingStatus.Bekreftet,
            EmployeeId = IngridId,
            ServiceId = HerreklippId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);

        var outcome = await service.UpdateStatusAsync(booking.Id, KlippOgStil, BookingStatus.Avlyst);

        Assert.Equal(UpdateBookingStatusOutcome.InvalidTransition, outcome);
    }

    // check if an absurd date gives no slots instead of a crash
    [Fact]
    public async Task GetFreeSlotsAsync_ExtremeDate_ReturnsNoSlots()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);

        var slots = await service.GetFreeSlotsAsync(HerreklippId, IngridId, new DateOnly(9999, 12, 31));

        Assert.NotNull(slots);
        Assert.Empty(slots);
    }
    // check if the list shows the average stars and the number of reviews for each business
    [Fact]
    public async Task GetAllAsync_ShowsAverageRatingAndReviewCount()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        foreach (var stars in new[] { 5, 4, 5 })
        {
            db.Reviews.Add(new Review
            {
                Id = Guid.NewGuid(), BookingId = Guid.NewGuid(), BusinessId = klippId, CustomerUserId = "kunde",
                CustomerName = "Kari", Stars = stars, CreatedUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        var result = await service.GetAllAsync();

        var klipp = result.Single(b => b.Id == klippId);
        Assert.Equal(4.7, klipp.AverageRating);
        Assert.Equal(3, klipp.ReviewCount);
        var fjord = result.Single(b => b.Name == "Fjord Bygg AS");
        Assert.Null(fjord.AverageRating);
        Assert.Equal(0, fjord.ReviewCount);
    }
    
    // check if a start time sent with a time zone (+02:00 arrives as local time) is booked, not a server error
    [Fact]
    public async Task CreateAsync_StartTimeAsLocalTime_IsBooked()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var request = HerreklippHosIngrid() with { StartUtc = FutureMondayAt10.ToLocalTime() };

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingOutcome.Created, result.Outcome);
        Assert.Equal(FutureMondayAt10, result.Booking!.StartUtc);
    }
}