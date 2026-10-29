using besti2.Application.Businesses;
using besti2.Application.Bookings;
using besti2.Application.Reviews;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// "Kunder" with notes, the lunch break, the CSV export, "Snart tid!", "Nyttig" and the employee's own login
public class CustomersAndStaffTests
{
    // Monday 7 January 2030, 10:00 in Oslo
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Ingrid = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid Sofie = new("b1111111-0000-0000-0000-000000000002");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Booking NewBooking(DateTime startUtc, string? userId = "kunde-1", string phone = "+4791234567",
        BookingStatus status = BookingStatus.Bekreftet, Guid? employeeId = null, string name = "Kari Nordmann") => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = startUtc.AddDays(-3),
        CustomerName = name,
        CustomerPhone = phone,
        CustomerEmail = userId is null ? "" : "kari@test.example",
        CustomerUserId = userId,
        Status = status,
        PriceNok = 450,
        ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
        EmployeeId = employeeId ?? Ingrid
    };

    // check if visits are grouped by account or phone, the note is saved and shown next to the visit in the calendar
    [Fact]
    public async Task CustomerCards_GroupVisitsAndKeepTheNote()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.AddRange(
            NewBooking(Now.AddDays(-30)),
            NewBooking(Now.AddDays(-10), status: BookingStatus.MotteIkke),
            NewBooking(Now.AddDays(2)),
            // The same phone in another format, without an account: a visit added by phone
            NewBooking(Now.AddDays(-5), userId: null, phone: "912 34 567", name: "Per Hansen"),
            NewBooking(Now.AddDays(-4), userId: null, phone: "+47 91234567", name: "Per Hansen"));
        await db.SaveChangesAsync();
        var cards = new CustomerCardService(db);

        var rows = await cards.ListAsync(KlippOgStil, null, Now);
        Assert.Equal(2, rows.Count);
        var kari = rows.Single(r => r.Key == "k-kunde-1");
        var per = rows.Single(r => r.Key == "t-91234567");
        Assert.Equal(1, kari.Visits);
        Assert.Equal(1, kari.NoShows);
        Assert.NotNull(kari.NextVisitUtc);
        Assert.Equal(2, per.Visits);
        Assert.Single(await cards.ListAsync(KlippOgStil, "per", Now));

        Assert.Null(await cards.SetNoteAsync(KlippOgStil, "k-kunde-1", "  Liker det kort i nakken  "));
        Assert.NotNull(await cards.SetNoteAsync(KlippOgStil, "k-ukjent", "x"));
        Assert.NotNull(await cards.SetNoteAsync(FjordBygg, "k-kunde-1", "x"));
        var card = await cards.GetAsync(KlippOgStil, "k-kunde-1", Now);
        Assert.Equal("Liker det kort i nakken", card!.Note);
        Assert.Equal(450, card.SpentNok);
        Assert.Equal(3, card.Visits.Count);
        Assert.Null(await cards.GetAsync(FjordBygg, "k-kunde-1", Now));

        var calendar = await new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance)
            .GetForBusinessAsync(KlippOgStil, DateOnly.FromDateTime(Now.AddDays(2)), DateOnly.FromDateTime(Now.AddDays(2)));
        var visit = Assert.Single(calendar);
        Assert.Equal("k-kunde-1", visit.CustomerKey);
        Assert.Equal("Liker det kort i nakken", visit.SalonNote);

        Assert.Null(await cards.SetNoteAsync(KlippOgStil, "k-kunde-1", ""));
        Assert.Empty(db.CustomerNotes);
    }

    // check if the lunch break is never offered as a free time, and a break outside the opening hours is refused
    [Fact]
    public void Break_IsNotFree()
    {
        var date = new DateOnly(2030, 1, 8);
        var slots = SlotCalculator.GetFreeSlots(date, new TimeOnly(9, 0), new TimeOnly(14, 0), 30, [], Now,
            new TimeOnly(12, 0), new TimeOnly(12, 30));
        var times = slots.Select(s => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(s, SlotCalculator.Oslo)))
            .ToList();

        Assert.Contains(new TimeOnly(11, 30), times);
        Assert.DoesNotContain(new TimeOnly(12, 0), times);
        Assert.Contains(new TimeOnly(12, 30), times);
    }

    [Fact]
    public async Task OpeningHours_BreakMustBeInside()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var monday = new OpeningHoursDto(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0),
            new TimeOnly(12, 0), new TimeOnly(12, 30));

        Assert.Null(await service.UpdateOpeningHoursAsync(KlippOgStil, new UpdateOpeningHoursRequest([monday])));
        Assert.NotNull(await service.UpdateOpeningHoursAsync(KlippOgStil, new UpdateOpeningHoursRequest(
            [monday with { BreakStart = new TimeOnly(8, 0) }])));
        Assert.NotNull(await service.UpdateOpeningHoursAsync(KlippOgStil, new UpdateOpeningHoursRequest(
            [monday with { BreakEnd = null }])));
        var saved = await db.OpeningHours.SingleAsync(h => h.BusinessId == KlippOgStil);
        Assert.Equal(new TimeOnly(12, 30), saved.BreakEnd);
    }

    // check if the CSV has Norwegian columns, comma decimals and never lets a name become an Excel formula
    [Fact]
    public async Task ExportCsv_SafeForExcel()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(NewBooking(Now.AddDays(-1), name: "=HYPERLINK(\"x\")"));
        await db.SaveChangesAsync();
        var export = new BookingExportService(db);

        var csv = await export.BookingsCsvAsync(KlippOgStil, new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 31));
        var lines = csv!.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.StartsWith("Dato;Fra;Til;Tjeneste", lines[0]);
        Assert.Contains("06.01.2030;10:00;10:30;Herreklipp;Ingrid Hansen;\"'=HYPERLINK(\"\"x\"\")\"", lines[1]);
        Assert.Contains(";450,00;På BESTI", lines[1]);
        Assert.Null(await export.BookingsCsvAsync(KlippOgStil, new DateOnly(2030, 1, 31), new DateOnly(2030, 1, 1)));
        Assert.Null(await export.BookingsCsvAsync(FjordBygg, new DateOnly(2020, 1, 1), new DateOnly(2030, 1, 1)));
    }

    // check if "Snart tid!" comes once about 2 hours before, not for a fresh booking, not when turned off
    [Fact]
    public async Task SoonNotice_OnceAndOnlyWhenWanted()
    {
        await using var db = await CreateSeededDbAsync();
        var soon = NewBooking(Now.AddMinutes(110));
        var fresh = NewBooking(Now.AddMinutes(100), userId: "kunde-2", employeeId: Sofie);
        fresh.CreatedUtc = Now.AddMinutes(-30);
        var later = NewBooking(Now.AddHours(5));
        var off = NewBooking(Now.AddMinutes(90), userId: "kunde-3", employeeId: Sofie);
        off.StartUtc = Now.AddMinutes(130);
        off.EndUtc = off.StartUtc.AddMinutes(30);
        db.Bookings.AddRange(soon, fresh, later, off);
        db.Users.Add(new AppUser { Id = "kunde-3", UserName = "k3@test.example", SoonNoticeOff = true });
        await db.SaveChangesAsync();
        var reminder = new BookingReminder(db, new FakeEmailService(), NullLogger<BookingReminder>.Instance);

        Assert.Equal(1, await reminder.SendSoonNoticesAsync(Now));
        Assert.Equal(0, await reminder.SendSoonNoticesAsync(Now.AddMinutes(15)));

        var notice = Assert.Single(db.Notifications);
        Assert.Equal("kunde-1", notice.UserId);
        Assert.Equal("Snart tid! Herreklipp hos Klipp & Stil Frisør kl. 11:50", notice.Text);
    }

    // check if the reminder e-mail the day before is skipped when the customer turned it off
    [Fact]
    public async Task ReminderEmail_TurnedOff_NotSent()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(NewBooking(Now.AddHours(20)));
        db.Users.Add(new AppUser { Id = "kunde-1", UserName = "kari@test.example", ReminderEmailOff = true });
        await db.SaveChangesAsync();
        var email = new FakeEmailService();

        var sent = await new BookingReminder(db, email, NullLogger<BookingReminder>.Instance).SendRemindersAsync(Now);

        Assert.Equal(0, sent);
        Assert.Empty(email.Sent);
    }

    // check if "Nyttig" goes on and off, is counted, and a customer cannot mark the own review
    [Fact]
    public async Task Helpful_TogglesAndCounts()
    {
        await using var db = await CreateSeededDbAsync();
        var booking = NewBooking(Now.AddDays(-2));
        var review = new Review
        {
            Id = Guid.NewGuid(), BookingId = booking.Id, BusinessId = KlippOgStil, CustomerUserId = "kunde-1",
            CustomerName = "Kari", Stars = 5, CreatedUtc = Now
        };
        db.Bookings.Add(booking);
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        Assert.Null(await service.ToggleHelpfulAsync("kunde-1", review.Id));
        Assert.Equal(new HelpfulResult(true, 1), await service.ToggleHelpfulAsync("kunde-2", review.Id));
        Assert.Equal(new HelpfulResult(true, 2), await service.ToggleHelpfulAsync("kunde-3", review.Id));
        var seen = await service.GetForBusinessAsync(KlippOgStil, viewerUserId: "kunde-2");
        Assert.Equal(new HelpfulResult(false, 1), await service.ToggleHelpfulAsync("kunde-2", review.Id));

        Assert.True(seen.Reviews[0].MarkedHelpful);
        Assert.Equal(2, seen.Reviews[0].Helpful);
    }

    // check if an employee connects only with the invited e-mail and then sees only their own visits
    [Fact]
    public async Task Staff_InviteAcceptAndOwnScheduleOnly()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.AddRange(
            new AppUser { Id = "ingrid", UserName = "ingrid@test.example", Email = "ingrid@test.example" },
            new AppUser { Id = "annen", UserName = "annen@test.example", Email = "annen@test.example" },
            new AppUser
            {
                Id = "eier", UserName = "eier@test.example", Email = "eier@test.example",
                NormalizedEmail = "EIER@TEST.EXAMPLE", BusinessId = KlippOgStil
            });
        db.Bookings.AddRange(
            NewBooking(Now.AddDays(1)),
            NewBooking(Now.AddDays(1).AddHours(1), employeeId: Sofie, name: "Ikke min kunde"));
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var staff = new StaffService(db, email, "https://bestiapp.no");

        Assert.NotNull(await staff.InviteAsync(KlippOgStil, Ingrid, "eier@test.example"));
        Assert.NotNull(await staff.InviteAsync(FjordBygg, Ingrid, "ingrid@test.example"));
        Assert.Null(await staff.InviteAsync(KlippOgStil, Ingrid, " Ingrid@Test.example "));
        var mail = Assert.Single(email.Sent);
        var token = Uri.UnescapeDataString(mail.Body.Split("kode=")[1].Split('\n')[0]);

        // The link works for a week from now
        var today = DateTime.UtcNow;
        Assert.Equal("Ingrid", (await staff.GetInviteAsync(token, today))!.EmployeeName);
        Assert.Null(await staff.GetInviteAsync(token, today.AddDays(8)));
        Assert.NotNull(await staff.AcceptAsync("annen", token, today));
        Assert.Null(await staff.AcceptAsync("ingrid", token, today));
        Assert.Null(await staff.GetInviteAsync(token, today));
        Assert.True(await staff.IsStaffAsync("ingrid"));

        var place = Assert.Single(await staff.GetScheduleAsync("ingrid", DateOnly.FromDateTime(Now), 7));
        Assert.Equal("Kari Nordmann", Assert.Single(place.Bookings).CustomerName);
        Assert.Empty(await staff.GetScheduleAsync("annen", DateOnly.FromDateTime(Now), 7));
        Assert.True((await staff.GetAccessAsync(KlippOgStil)).Single().Connected);

        Assert.True(await staff.RemoveAsync(KlippOgStil, Ingrid));
        Assert.False(await staff.IsStaffAsync("ingrid"));
    }
}
