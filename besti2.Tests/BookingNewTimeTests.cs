using System.Text.RegularExpressions;
using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class BookingNewTimeTests
{
    // Monday 5 Jan 2099 (Klipp & Stil is open 09–17); far in the future, so the test never gets old
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid SofieId = new("b1111111-0000-0000-0000-000000000002");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static DateTime Oslo(int hour) => SlotCalculator.ToUtc(FutureMonday, new TimeOnly(hour, 0));

    // Kari books online with her account (Venter), the business confirms it
    private static async Task<Guid> ConfirmedVisitAsync(BookingService service, string email = "kari@test.example")
    {
        var created = await service.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId, Oslo(10),
            "Kari Nordmann", "+4790000000", email, false), "kari-user");
        await service.UpdateStatusAsync(created.Booking!.Id, KlippOgStil, BookingStatus.Bekreftet);
        return created.Booking.Id;
    }

    private static MoveBookingRequest AskFor(Guid employeeId, int hour) =>
        new(employeeId, FutureMonday, new TimeOnly(hour, 0), NotifyCustomer: false, AskCustomer: true);

    // The one-time token from the link in the last e-mail
    private static string TokenFrom(FakeEmailService email) =>
        Regex.Match(email.Sent.Last().Body, @"ny-tid\?token=([A-Za-z0-9_-]+)").Groups[1].Value;

    // check if "Flytt" with the customer's answer moves the visit, waits for the customer and mails a link
    [Fact]
    public async Task Move_AskCustomer_WaitsAndMailsLink()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance, "https://bestiapp.no");
        var id = await ConfirmedVisitAsync(service);

        await service.MoveAsync(id, KlippOgStil, AskFor(SofieId, 13));

        var booking = await db.Bookings.SingleAsync(b => b.Id == id);
        Assert.Equal(BookingStatus.VenterKunde, booking.Status);
        Assert.Equal(Oslo(13), booking.StartUtc);
        Assert.Equal(Oslo(10), booking.PreviousStartUtc);
        var mail = email.Sent.Last();
        Assert.Equal("kari@test.example", mail.To);
        Assert.Contains("https://bestiapp.no/ny-tid?token=", mail.Body);
        var shown = await service.GetNewTimeAsync(TokenFrom(email));
        Assert.Equal(Oslo(13), shown!.StartUtc);
        Assert.Equal(Oslo(10), shown.PreviousStartUtc);
    }

    // check if Godta lets the business confirm next, tells the business, and the link works only once
    [Fact]
    public async Task Accept_ByLink_WaitsForBusinessAndLinkIsUsed()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var id = await ConfirmedVisitAsync(service);
        await service.MoveAsync(id, KlippOgStil, AskFor(IngridId, 13));
        var token = TokenFrom(email);

        var first = await service.AnswerNewTimeAsync(token, accept: true);
        var second = await service.AnswerNewTimeAsync(token, accept: false);

        var booking = await db.Bookings.SingleAsync(b => b.Id == id);
        Assert.Equal(NewTimeOutcome.Accepted, first);
        Assert.Equal(NewTimeOutcome.NotFound, second);
        Assert.Equal(BookingStatus.Venter, booking.Status);
        Assert.Null(booking.NewTimeTokenHash);
        Assert.Contains("har godtatt den nye tiden", email.Sent.Last().Body);
    }

    // check if Passer ikke cancels the visit, so the time is free for others again
    [Fact]
    public async Task Decline_ByLink_CancelsAndFreesTheTime()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var id = await ConfirmedVisitAsync(service);
        await service.MoveAsync(id, KlippOgStil, AskFor(IngridId, 13));

        var outcome = await service.AnswerNewTimeAsync(TokenFrom(email), accept: false);

        Assert.Equal(NewTimeOutcome.Declined, outcome);
        Assert.Equal(BookingStatus.Avlyst, (await db.Bookings.SingleAsync(b => b.Id == id)).Status);
        var free = await service.GetFreeSlotsAsync(HerreklippId, IngridId, FutureMonday);
        Assert.Contains(Oslo(13), free!);
    }

    // check if a customer without e-mail is moved at once (they cannot get the link)
    [Fact]
    public async Task Move_AskCustomerWithoutEmail_IsFinalAtOnce()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var created = await service.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(10, 0), "Ola", "912 34 567", null, null, false));

        await service.MoveAsync(created.Booking!.Id, KlippOgStil, AskFor(IngridId, 13));

        var booking = await db.Bookings.SingleAsync(b => b.Id == created.Booking.Id);
        Assert.Equal(BookingStatus.Bekreftet, booking.Status);
        Assert.Null(booking.NewTimeTokenHash);
    }

    // check if the answer from Min Besti works only for the customer's own booking
    [Fact]
    public async Task Answer_FromMinBesti_OnlyOwnBooking()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        var id = await ConfirmedVisitAsync(service);
        await service.MoveAsync(id, KlippOgStil, AskFor(IngridId, 13));

        var byOther = await service.AnswerNewTimeByCustomerAsync(id, "someone-else", accept: true);
        var byKari = await service.AnswerNewTimeByCustomerAsync(id, "kari-user", accept: true);

        Assert.Equal(NewTimeOutcome.NotFound, byOther);
        Assert.Equal(NewTimeOutcome.Accepted, byKari);
    }

    // check if moving twice before the answer keeps the original time as "before", and the old link stops working
    [Fact]
    public async Task Move_Twice_KeepsOriginalTimeAndNewLink()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var id = await ConfirmedVisitAsync(service);
        await service.MoveAsync(id, KlippOgStil, AskFor(IngridId, 13));
        var oldToken = TokenFrom(email);

        await service.MoveAsync(id, KlippOgStil, AskFor(IngridId, 15));

        Assert.Null(await service.GetNewTimeAsync(oldToken));
        var shown = await service.GetNewTimeAsync(TokenFrom(email));
        Assert.Equal(Oslo(15), shown!.StartUtc);
        Assert.Equal(Oslo(10), shown.PreviousStartUtc);
    }
}
