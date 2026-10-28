using besti2.Domain.Enums;
using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Identity;
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

    // check if the owner gets an e-mail about a new booking, with the customer and a link to the calendar
    [Fact]
    public async Task CreateAsync_OwnerGetsNewBookingEmail()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            EmailConfirmed = true, BusinessId = KlippOgStil
        });
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance, "https://bestiapp.no");

        var created = await service.CreateAsync(HerreklippHosIngrid());

        Assert.Equal(2, email.Sent.Count);
        var toOwner = email.Sent.Single(m => m.To == "eier@klippogstil.example");
        Assert.StartsWith("Ny bestilling: Herreklipp", toOwner.Subject);
        Assert.Contains("Kari Nordmann", toOwner.Body);
        Assert.Contains("+4790000000", toOwner.Body);
        Assert.Contains($"https://bestiapp.no/panel?dag=2099-01-05&bestilling={created.Booking!.Id}", toOwner.Body);
    }

    // check if an owner who never confirmed the e-mail gets nothing (nobody knows the address is right)
    [Fact]
    public async Task CreateAsync_UnconfirmedOwner_GetsNoEmail()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            EmailConfirmed = false, BusinessId = KlippOgStil
        });
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);

        await service.CreateAsync(HerreklippHosIngrid());

        Assert.Equal("kari@test.example", Assert.Single(email.Sent).To);
    }

    // check if a time less than an hour away cannot be booked online (the salon must have time to see it)
    [Fact]
    public async Task CreateAsync_StartsWithinAnHour_IsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var soon = DateTime.UtcNow.AddMinutes(30);

        var result = await service.CreateAsync(HerreklippHosIngrid() with { StartUtc = soon }, "kunde-1");

        Assert.Equal(CreateBookingOutcome.Invalid, result.Outcome);
        Assert.Contains("minst én time", result.Error);
    }

    // check if one account can have at most 3 coming visits at the same business
    [Fact]
    public async Task CreateAsync_FourthUpcomingAtSameBusiness_IsInvalid()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        for (var i = 0; i < BookingRules.MaxUpcomingPerBusiness; i++)
        {
            var created = await service.CreateAsync(
                HerreklippHosIngrid() with { StartUtc = FutureMondayAt10.AddMinutes(30 * i) }, "kunde-1");
            Assert.Equal(CreateBookingOutcome.Created, created.Outcome);
        }

        var fourth = await service.CreateAsync(
            HerreklippHosIngrid() with { StartUtc = FutureMondayAt10.AddHours(2) }, "kunde-1");
        var otherCustomer = await service.CreateAsync(
            HerreklippHosIngrid() with { StartUtc = FutureMondayAt10.AddHours(2) }, "kunde-2");

        Assert.Equal(CreateBookingOutcome.Invalid, fourth.Outcome);
        Assert.Contains("3 kommende timer", fourth.Error);
        Assert.Equal(CreateBookingOutcome.Created, otherCustomer.Outcome);
    }

    // check if a business with "Bekreft automatisk" gets the booking confirmed at once, and the customer is told
    [Fact]
    public async Task CreateAsync_AutoConfirm_IsConfirmedAtOnce()
    {
        await using var db = await CreateSeededDbAsync();
        (await db.Businesses.SingleAsync(b => b.Id == KlippOgStil)).AutoConfirm = true;
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);

        var result = await service.CreateAsync(HerreklippHosIngrid(), "kunde-1");

        Assert.Equal("Bekreftet", result.Booking!.Status);
        Assert.Equal(BookingStatus.Bekreftet, (await db.Bookings.SingleAsync()).Status);
        Assert.StartsWith("Bestilling bekreftet", Assert.Single(email.Sent).Subject);
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
        var first = await service.CreateAsync(HerreklippHosIngrid() with
        {
            StartUtc = FutureMondayAt10.AddMinutes(30)
        });
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

    // check if the calendar gets only the bookings of the chosen days, with the employee for the column
    [Fact]
    public async Task GetForBusinessAsync_WithDays_ReturnsOnlyThoseDays()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        await service.CreateAsync(HerreklippHosIngrid()); // Monday
        await service.CreateAsync(HerreklippHosIngrid() with
        {
            StartUtc = FutureMondayAt10.AddDays(7)
        }); // a week later
        var monday = DateOnly.FromDateTime(FutureMondayAt10);

        var result = await service.GetForBusinessAsync(KlippOgStil, monday, monday);

        var booking = Assert.Single(result);
        Assert.Equal(FutureMondayAt10, booking.StartUtc);
        Assert.Equal(IngridId, booking.EmployeeId);
    }

    // check if the same customer account cannot book two visits at the same time
    [Fact]
    public async Task CreateAsync_SameCustomerOverlapping_IsCustomerBusy()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var withSofie = HerreklippHosIngrid() with { EmployeeId = new Guid("b1111111-0000-0000-0000-000000000002") };

        var first = await service.CreateAsync(HerreklippHosIngrid(), "user-ada");
        var second = await service.CreateAsync(withSofie, "user-ada");

        Assert.Equal(CreateBookingOutcome.Created, first.Outcome);
        Assert.Equal(CreateBookingOutcome.CustomerBusy, second.Outcome);
    }

    // check if another customer can still book the same time with another employee
    [Fact]
    public async Task CreateAsync_OtherCustomerSameTime_IsCreated()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var withSofie = HerreklippHosIngrid() with { EmployeeId = new Guid("b1111111-0000-0000-0000-000000000002") };

        var first = await service.CreateAsync(HerreklippHosIngrid(), "user-ada");
        var second = await service.CreateAsync(withSofie, "user-michal");

        Assert.Equal(CreateBookingOutcome.Created, first.Outcome);
        Assert.Equal(CreateBookingOutcome.Created, second.Outcome);
    }
}