using besti2.Application.Bookings;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class BookingCancelMessageTests
{
    // Monday 5 Jan 2099, 10:00 in Oslo; far in the future, so the test never gets old
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

    private static async Task<Guid> VisitAsync(BookingService service)
    {
        var result = await service.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(10, 0), "Ola", "912 34 567", "ola@test.example", null, false));
        return result.Booking!.Id;
    }

    // check if the cancel mail has the business's message and a link to book a new time with that business
    [Fact]
    public async Task Cancel_WithMessage_MailHasMessageAndBookingLink()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance, "https://bestiapp.no");
        var id = await VisitAsync(service);

        await service.UpdateStatusAsync(id, KlippOgStil, BookingStatus.Avlyst, "  Beklager, Ingrid er syk.  ");

        var mail = Assert.Single(email.Sent);
        Assert.Contains("Melding fra Klipp & Stil Frisør: Beklager, Ingrid er syk.", mail.Body);
        Assert.Contains("Bestill ny tid: https://bestiapp.no/klipp-og-stil-frisor-oslo", mail.Body);
    }

    // check if a message is never sent with a confirmation, and a long message is cut to 300 characters
    [Fact]
    public async Task Message_OnlyWhenCancelling_AndAtMost300()
    {
        await using var db = await CreateSeededDbAsync();
        var email = new FakeEmailService();
        var service = new BookingService(db, email, NullLogger<BookingService>.Instance);
        var id = await VisitAsync(service);
        var other = await service.CreateByBusinessAsync(KlippOgStil, new CreateManualBookingRequest(HerreklippId,
            IngridId, FutureMonday, new TimeOnly(12, 0), "Per", "912 34 567", "per@test.example", null, false));

        await service.UpdateStatusAsync(other.Booking!.Id, KlippOgStil, BookingStatus.Avlyst, new string('x', 400));
        await service.UpdateStatusAsync(id, KlippOgStil, BookingStatus.Avlyst);

        Assert.Contains("Melding fra Klipp & Stil Frisør: " + new string('x', 300) + "\n", email.Sent[0].Body);
        Assert.DoesNotContain("Melding fra", email.Sent[1].Body);
        Assert.Contains("Bestill ny tid:", email.Sent[1].Body);
    }
}