using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class BookingReminderTests
{
    // Monday 7 January 2030, 10:00 in Oslo
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);

    private static Booking NewBooking(DateTime startUtc, BookingStatus status = BookingStatus.Bekreftet,
        string email = "kunde@test.example", DateTime? createdUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = createdUtc ?? Now.AddDays(-3),
        CustomerName = "Ola",
        CustomerPhone = "+4790000000",
        CustomerEmail = email,
        CustomerUserId = "kunde-1",
        Status = status,
        ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
        EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001")
    };

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    // check if a confirmed visit tomorrow gets one reminder with the details, and the next run sends nothing
    [Fact]
    public async Task SendRemindersAsync_ConfirmedVisitTomorrow_GetsOneReminder()
    {
        await using var db = await CreateSeededDbAsync();
        var booking = NewBooking(Now.AddHours(23));
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance, "https://bestiapp.no");

        var first = await reminder.SendRemindersAsync(Now);
        var second = await reminder.SendRemindersAsync(Now.AddMinutes(15));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var mail = Assert.Single(email.Sent);
        Assert.Equal("kunde@test.example", mail.To);
        Assert.Equal("Påminnelse: time i morgen kl. 09:00 hos Klipp & Stil Frisør", mail.Subject);
        Assert.Contains("Herreklipp", mail.Body);
        Assert.Contains("Ingrid Hansen", mail.Body);
        Assert.Contains("https://bestiapp.no/min-besti", mail.Body);
        Assert.Equal(Now, (await db.Bookings.SingleAsync()).ReminderSentUtc);
    }

    // check if Venter, Avlyst, Møtte ikke, no e-mail, past visits and visits later than 24 hours get nothing
    [Fact]
    public async Task SendRemindersAsync_SkipsVisitsThatShouldNotGetReminder()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.AddRange(
            NewBooking(Now.AddHours(20), BookingStatus.Venter),
            NewBooking(Now.AddHours(20).AddMinutes(30), BookingStatus.Avlyst),
            NewBooking(Now.AddHours(-1), BookingStatus.MotteIkke),
            NewBooking(Now.AddHours(21), email: ""),
            NewBooking(Now.AddHours(-2)),
            NewBooking(Now.AddHours(25)));
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance);

        var count = await reminder.SendRemindersAsync(Now);

        Assert.Equal(0, count);
        Assert.Empty(email.Sent);
    }

    // check if a visit booked less than 12 hours before it starts gets no reminder
    [Fact]
    public async Task SendRemindersAsync_BookedShortlyBefore_GetsNothing()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(NewBooking(Now.AddHours(5), createdUtc: Now.AddHours(-1)));
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance);

        var count = await reminder.SendRemindersAsync(Now);

        Assert.Equal(0, count);
        Assert.Empty(email.Sent);
    }

    // check if a visit later today says "i dag", and a passed free-cancel deadline tells the customer to call
    [Fact]
    public async Task SendRemindersAsync_LaterToday_SaysIDag_AndCallAfterDeadline()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(NewBooking(Now.AddHours(6)));
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance);

        await reminder.SendRemindersAsync(Now);

        var mail = Assert.Single(email.Sent);
        Assert.StartsWith("Påminnelse: time i dag kl. 16:00", mail.Subject);
        Assert.Contains("Fristen for gratis avbestilling har gått ut", mail.Body);
    }

    // check if a mail server that is down does not throw and the reminder is tried again next time
    [Fact]
    public async Task SendRemindersAsync_MailFails_TriesAgainLater()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(NewBooking(Now.AddHours(23)));
        await db.SaveChangesAsync();
        var failing = new BookingReminder(db, new FailingEmailService(), NullLogger<BookingReminder>.Instance);

        var count = await failing.SendRemindersAsync(Now);

        Assert.Equal(0, count);
        Assert.Null((await db.Bookings.SingleAsync()).ReminderSentUtc);
        var working = new BookingReminder(db, new FakeEmailService(), NullLogger<BookingReminder>.Instance);
        Assert.Equal(1, await working.SendRemindersAsync(Now.AddMinutes(15)));
    }

    // check if a visit added by the business without "Send bekreftelse" gets no reminder
    [Fact]
    public async Task SendRemindersAsync_NoReminder_GetsNothing()
    {
        await using var db = await CreateSeededDbAsync();
        var booking = NewBooking(Now.AddHours(23));
        booking.NoReminder = true;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance);

        Assert.Equal(0, await reminder.SendRemindersAsync(Now));
        Assert.Empty(email.Sent);
    }
    // check if a mail server that keeps failing is tried 3 times and then left alone
    [Fact]
    public async Task SendRemindersAsync_StopsAfterThreeFailures()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(NewBooking(Now.AddHours(23)));
        await db.SaveChangesAsync();
        var failing = new BookingReminder(db, new FailingEmailService(), NullLogger<BookingReminder>.Instance);

        for (var run = 0; run < 3; run++)
        {
            await failing.SendRemindersAsync(Now.AddMinutes(15 * run));
        }

        Assert.Equal(3, (await db.Bookings.SingleAsync()).ReminderAttempts);
        var email = new FakeEmailService();
        var working = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance);
        Assert.Equal(0, await working.SendRemindersAsync(Now.AddMinutes(45)));
        Assert.Empty(email.Sent);
    }


    // check if a booking still waiting for "Bekreft" after 2 hours gives the owner one e-mail, and only once
    [Fact]
    public async Task SendPendingRemindersAsync_WaitingTwoHours_OwnerGetsOneMail()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            EmailConfirmed = true, BusinessId = new Guid("11111111-1111-1111-1111-111111111111")
        });
        db.Bookings.Add(NewBooking(Now.AddDays(2), BookingStatus.Venter, createdUtc: Now.AddHours(-3)));
        db.Bookings.Add(NewBooking(Now.AddDays(3), BookingStatus.Venter, createdUtc: Now.AddHours(-5)));
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance, "https://bestiapp.no");

        var first = await reminder.SendPendingRemindersAsync(Now);
        var second = await reminder.SendPendingRemindersAsync(Now.AddMinutes(15));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var mail = Assert.Single(email.Sent);
        Assert.Equal("eier@klippogstil.example", mail.To);
        Assert.Equal("2 bestillinger venter på bekreftelse", mail.Subject);
        Assert.Contains("https://bestiapp.no/panel/liste", mail.Body);
    }

    // check if a booking made less than 2 hours ago, or already confirmed, gives no reminder yet
    [Fact]
    public async Task SendPendingRemindersAsync_JustBookedOrConfirmed_NoMail()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            EmailConfirmed = true, BusinessId = new Guid("11111111-1111-1111-1111-111111111111")
        });
        db.Bookings.Add(NewBooking(Now.AddDays(2), BookingStatus.Venter, createdUtc: Now.AddHours(-1)));
        db.Bookings.Add(NewBooking(Now.AddDays(3), BookingStatus.Bekreftet, createdUtc: Now.AddHours(-5)));
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var reminder = new BookingReminder(db, email, NullLogger<BookingReminder>.Instance);

        var sent = await reminder.SendPendingRemindersAsync(Now);

        Assert.Equal(0, sent);
        Assert.Empty(email.Sent);
    }
}
