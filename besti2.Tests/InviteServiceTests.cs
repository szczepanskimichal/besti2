using besti2.Application.Bookings;
using besti2.Application.Common;
using besti2.Application.Invites;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using besti2.Infrastructure.Services.Invites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// BESTI sammen: send, look at, accept and decline an invitation (two people and groups)
public class InviteServiceTests
{
    private static readonly DateTime FutureMondayAt10 = new(2099, 1, 5, 9, 0, 0, DateTimeKind.Utc); // 10:00 in Oslo
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid SnekkerarbeidId = new("a2222222-0000-0000-0000-000000000003"); // only Lars
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private static readonly Guid SofieId = new("b1111111-0000-0000-0000-000000000002");

    private const string KariId = "kari";
    private const string OlaId = "ola";
    private const string PerId = "per";
    private const string LiseId = "lise";
    private static readonly Guid KlippOgStilId = new("11111111-1111-1111-1111-111111111111");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Users.AddRange(
            new AppUser { Id = KariId, UserName = "kari@test.example", Email = "kari@test.example", FullName = "Kari Nordmann" },
            new AppUser { Id = OlaId, UserName = "ola@test.example", Email = "ola@test.example", FullName = "Ola Hansen" },
            new AppUser { Id = PerId, UserName = "per@test.example", Email = "per@test.example", FullName = "Per Olsen" },
            new AppUser { Id = LiseId, UserName = "lise@test.example", Email = "lise@test.example", FullName = "Lise Berg" },
            new AppUser
            {
                Id = "eier", UserName = "eier@test.example", Email = "eier@test.example", FullName = "Eier",
                BusinessId = KlippOgStilId, EmailConfirmed = true
            });
        // Klipp & Stil has two employees in the seed data; a group of four needs four
        db.Employees.AddRange(
            new Domain.Entities.Employee { Id = Guid.NewGuid(), Name = "Mona", LastName = "Lie", BusinessId = KlippOgStilId },
            new Domain.Entities.Employee { Id = Guid.NewGuid(), Name = "Nils", LastName = "Dahl", BusinessId = KlippOgStilId });
        await db.SaveChangesAsync();
        return db;
    }

    private static InviteService Invites(AppDbContext db, FakeEmailService? mail = null) =>
        new(db, mail ?? new FakeEmailService(), NullLogger<InviteService>.Instance);

    private static CreateInviteRequest Request(Guid? employeeId = null, Guid? serviceId = null,
        string payMode = "HverSelv", int minutes = 30, int persons = 2, bool organizerJoins = true) =>
        new(serviceId ?? HerreklippId, employeeId, FutureMondayAt10, "912 34 567", payMode, minutes, "Vi kommer sammen",
            Persons: persons, OrganizerJoins: organizerJoins);

    private static async Task<string> SendAsync(AppDbContext db, Guid? employeeId = null)
    {
        var result = await Invites(db).CreateAsync(Request(employeeId), KariId);
        Assert.Equal(InviteOutcome.Created, result.Outcome);
        return result.Token!;
    }

    // check if sending stores only the hash of the link token, and books nothing yet
    [Fact]
    public async Task Create_StoresHashOnly_NothingBooked()
    {
        await using var db = await CreateSeededDbAsync();

        var token = await SendAsync(db);

        var invite = await db.BookingInvites.SingleAsync();
        Assert.Equal(SecureToken.Hash(token), invite.TokenHash);
        Assert.Equal(InviteStatus.Venter, invite.Status);
        Assert.Empty(db.Bookings);
    }

    // check if a business with one employee cannot get an invitation for two
    [Fact]
    public async Task Create_OneEmployee_SlotTaken()
    {
        await using var db = await CreateSeededDbAsync();

        var result = await Invites(db).CreateAsync(Request(serviceId: SnekkerarbeidId), KariId);

        Assert.Equal(InviteOutcome.SlotTaken, result.Outcome);
    }

    // check if a strange "who pays" or answer time is refused
    [Theory]
    [InlineData("Alle", 30)]
    [InlineData("5", 30)]
    [InlineData("HverSelv", 45)]
    public async Task Create_InvalidChoice_Invalid(string payMode, int minutes)
    {
        await using var db = await CreateSeededDbAsync();

        var result = await Invites(db).CreateAsync(Request(payMode: payMode, minutes: minutes), KariId);

        Assert.Equal(InviteOutcome.Invalid, result.Outcome);
    }

    // check if one customer cannot have more than 3 invitations waiting at the same time
    [Fact]
    public async Task Create_FourthWaiting_Invalid()
    {
        await using var db = await CreateSeededDbAsync();
        for (var i = 0; i < InviteService.MaxWaitingPerCustomer; i++)
        {
            await SendAsync(db);
        }

        var result = await Invites(db).CreateAsync(Request(), KariId);

        Assert.Equal(InviteOutcome.Invalid, result.Outcome);
    }

    // check if the link shows only the first name, and knows who is looking
    [Fact]
    public async Task Get_ShowsFirstNameAndWhoIsLooking()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);

        var stranger = await Invites(db).GetAsync(token, null);
        var inviter = await Invites(db).GetAsync(token, KariId);

        Assert.NotNull(stranger);
        Assert.Equal("Kari", stranger.InviterFirstName);
        Assert.Equal("Venter", stranger.Status);
        Assert.True(stranger.StillFree);
        Assert.False(stranger.IsInviter);
        Assert.True(inviter!.IsInviter);
        Assert.Null(await Invites(db).GetAsync("feil-lenke", null));
    }

    // check if "Jeg blir med!" books two visits with two employees, Kari keeps Sofie, and Kari gets the bell
    [Fact]
    public async Task Accept_BooksBothWithTheSameInviteId()
    {
        await using var db = await CreateSeededDbAsync();
        var mail = new FakeEmailService();
        var token = await SendAsync(db, SofieId);

        var result = await Invites(db, mail).AcceptAsync(token, OlaId, new AcceptInviteRequest("+47 400 00 000"));

        Assert.Equal(InviteOutcome.Accepted, result.Outcome);
        var invite = await db.BookingInvites.SingleAsync();
        var bookings = await db.Bookings.Where(b => b.InviteId == invite.Id).ToListAsync();
        Assert.Equal(2, bookings.Count);
        var kari = Assert.Single(bookings, b => b.CustomerUserId == KariId);
        var ola = Assert.Single(bookings, b => b.CustomerUserId == OlaId);
        Assert.Equal(SofieId, kari.EmployeeId);
        Assert.Equal(IngridId, ola.EmployeeId);
        Assert.Equal("912 34 567", kari.CustomerPhone);
        Assert.Equal("Vi kommer sammen", kari.CustomerNote);
        Assert.Equal("Ola Hansen", ola.CustomerName);
        Assert.Equal(InviteStatus.Akseptert, invite.Status);
        Assert.NotNull(invite.ClosedUtc); // two places, both taken: closed at once
        Assert.Equal("-", invite.InviterPhone); // copied to the booking, not kept twice
        Assert.Contains(db.Notifications, n => n.UserId == KariId && n.Text.StartsWith("Ola blir med!"));
        Assert.Contains(mail.Sent, m => m.To == "kari@test.example");
        Assert.Contains(mail.Sent, m => m.To == "ola@test.example");
    }

    // check if the inviter cannot accept their own invitation
    [Fact]
    public async Task Accept_OwnInvite_Refused()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);

        var result = await Invites(db).AcceptAsync(token, KariId, new AcceptInviteRequest("91234567"));

        Assert.Equal(InviteOutcome.OwnInvite, result.Outcome);
        Assert.Empty(db.Bookings);
    }

    // check if a second "Jeg blir med!" does not book anything more
    [Fact]
    public async Task Accept_Twice_AlreadyJoined()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        var again = await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        Assert.Equal(InviteOutcome.AlreadyJoined, again.Outcome);
        Assert.Equal(2, await db.Bookings.CountAsync());
    }

    // check if an answer after the deadline books nothing, and Kari is told once
    [Fact]
    public async Task Accept_AfterDeadline_ExpiredAndInviterTold()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        var invite = await db.BookingInvites.SingleAsync();
        invite.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var result = await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));
        await Invites(db).GetAsync(token, null); // reading it again must not tell Kari twice

        Assert.Equal(InviteOutcome.Expired, result.Outcome);
        Assert.Equal(InviteStatus.Utlopt, invite.Status);
        Assert.Empty(db.Bookings);
        Assert.Single(db.Notifications, n => n.UserId == KariId && n.Text.StartsWith("Ingen svar"));
    }

    // check if a time taken in the meantime is refused and nothing is half booked
    [Fact]
    public async Task Accept_TimeTakenMeanwhile_SlotTaken()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        // Three of the four hairdressers get other customers: only one is left, too few for two
        foreach (var employee in await db.Employees.Where(e => e.BusinessId == KlippOgStilId && e.Id != SofieId)
                     .ToListAsync())
        {
            await new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance)
                .CreateAsync(new CreateBookingRequest(HerreklippId, employee.Id, FutureMondayAt10,
                    "Per Persen", "+4790000000", "per@test.example", false));
        }

        var preview = await Invites(db).GetAsync(token, OlaId);
        var result = await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        Assert.False(preview!.StillFree);
        Assert.Equal(InviteOutcome.SlotTaken, result.Outcome);
        Assert.Equal(3, await db.Bookings.CountAsync());
        Assert.Equal(InviteStatus.Venter, (await db.BookingInvites.SingleAsync()).Status);
    }

    // check if the friend cannot accept when they already have a visit at that time
    [Fact]
    public async Task Accept_FriendBusy_CustomerBusy()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        await new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance)
            .CreateAsync(new CreateBookingRequest(new Guid("a2222222-0000-0000-0000-000000000001"), // Fjord Bygg
                new Guid("b2222222-0000-0000-0000-000000000001"), FutureMondayAt10.AddMinutes(-30),
                "Ola Hansen", "+4790000000", "ola@test.example", false), OlaId);

        var result = await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        Assert.Equal(InviteOutcome.CustomerBusy, result.Outcome);
    }

    // check if "Nei takk" works without login and Kari gets the bell
    [Fact]
    public async Task Decline_WithoutLogin_InviterTold()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);

        var result = await Invites(db).DeclineAsync(token, null);

        Assert.Equal(InviteOutcome.Declined, result.Outcome);
        Assert.Equal(InviteStatus.Avslatt, (await db.BookingInvites.SingleAsync()).Status);
        Assert.Contains(db.Notifications, n => n.UserId == KariId && n.Text.StartsWith("En venn kan dessverre"));
    }

    // check if a withdrawn invitation cannot be accepted
    [Fact]
    public async Task Withdraw_ThenAccept_AlreadyAnswered()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        var id = (await db.BookingInvites.SingleAsync()).Id;

        var withdrawn = await Invites(db).WithdrawAsync(id, KariId);
        var accept = await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        Assert.Equal(InviteOutcome.Declined, withdrawn.Outcome);
        Assert.Equal(InviteOutcome.AlreadyAnswered, accept.Outcome);
    }

    // check if only the inviter can take it back
    [Fact]
    public async Task Withdraw_SomeoneElse_NotFound()
    {
        await using var db = await CreateSeededDbAsync();
        await SendAsync(db);
        var id = (await db.BookingInvites.SingleAsync()).Id;

        var result = await Invites(db).WithdrawAsync(id, OlaId);

        Assert.Equal(InviteOutcome.NotFound, result.Outcome);
    }

    // check if "Del igjen" gives a new link and the old one stops working
    [Fact]
    public async Task NewLink_OldLinkStopsWorking()
    {
        await using var db = await CreateSeededDbAsync();
        var oldToken = await SendAsync(db);
        var id = (await db.BookingInvites.SingleAsync()).Id;

        var result = await Invites(db).NewLinkAsync(id, KariId);

        Assert.Equal(InviteOutcome.Created, result.Outcome);
        Assert.Null(await Invites(db).GetAsync(oldToken, null));
        Assert.NotNull(await Invites(db).GetAsync(result.Token!, null));
    }

    // check if Min Besti shows the invitation to both, with everyone's first name
    [Fact]
    public async Task GetForCustomer_BothSeeIt()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        var kari = Assert.Single(await Invites(db).GetForCustomerAsync(KariId));
        var ola = Assert.Single(await Invites(db).GetForCustomerAsync(OlaId));

        Assert.True(kari.IsInviter);
        Assert.Equal(["Kari", "Ola"], kari.Names);
        Assert.False(ola.IsInviter);
        Assert.Equal("Kari", ola.InviterFirstName);
        Assert.Equal("Akseptert", ola.Status);
        Assert.Equal(2, ola.Joined);
    }

    // check if the calendar shows one group: Kari organizes and pays for everyone
    [Fact]
    public async Task Panel_JegSpanderer_ShowsTheGroup()
    {
        await using var db = await CreateSeededDbAsync();
        var sent = await Invites(db).CreateAsync(Request(payMode: "JegSpanderer", persons: 3), KariId);
        await Invites(db).AcceptAsync(sent.Token!, OlaId, new AcceptInviteRequest("91234567"));

        var calendar = await new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance)
            .GetForBusinessAsync(KlippOgStilId, new DateOnly(2099, 1, 5), new DateOnly(2099, 1, 6));

        Assert.Equal(2, calendar.Count);
        var kari = Assert.Single(calendar, b => b.CustomerName == "Kari Nordmann").Group!;
        var ola = Assert.Single(calendar, b => b.CustomerName == "Ola Hansen").Group!;
        Assert.True(kari.IsOrganizer);
        Assert.False(ola.IsOrganizer);
        Assert.True(ola.OrganizerPays);
        Assert.Equal(3, ola.Persons);
        Assert.Equal(2, ola.Joined);
        Assert.True(ola.Open); // one place left, still time to answer
        Assert.Equal(["Kari Nordmann", "Ola Hansen"], ola.Names);
    }

    // check if a group of four fills up one by one, says "3 av 4" to the business, and closes when full
    [Fact]
    public async Task Group_JoinOneByOne_ClosesWhenFull()
    {
        await using var db = await CreateSeededDbAsync();
        var mail = new FakeEmailService();
        var token = (await Invites(db).CreateAsync(Request(persons: 4), KariId)).Token!;

        Assert.Equal(InviteOutcome.Accepted,
            (await Invites(db, mail).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"))).Outcome);
        Assert.Equal(InviteOutcome.Accepted,
            (await Invites(db, mail).AcceptAsync(token, PerId, new AcceptInviteRequest("91234568"))).Outcome);
        var beforeLast = await Invites(db).GetAsync(token, LiseId);
        Assert.Equal(InviteOutcome.Accepted,
            (await Invites(db, mail).AcceptAsync(token, LiseId, new AcceptInviteRequest("91234569"))).Outcome);

        Assert.Equal(1, beforeLast!.PlacesLeft);
        Assert.True(beforeLast.Open);
        Assert.Equal(4, await db.Bookings.CountAsync());
        Assert.Equal(4, await db.Bookings.Select(b => b.EmployeeId).Distinct().CountAsync());
        Assert.Contains(db.Notifications, n => n.BusinessId == KlippOgStilId && n.Text.Contains("Per Olsen") && n.Text.Contains("(3 av 4)"));
        Assert.Contains(db.Notifications, n => n.BusinessId == KlippOgStilId && n.Text.Contains("er klar: 4 personer"));
        Assert.NotNull((await db.BookingInvites.SingleAsync()).ClosedUtc);
        // One e-mail to the business with the final count, not one per person
        Assert.Single(mail.Sent, m => m.To == "eier@test.example");
        var after = await Invites(db).GetAsync(token, null);
        Assert.False(after!.Open);
        Assert.Equal(["Kari", "Ola", "Per", "Lise"], after.Names);
    }

    // check if a full group says so to the next one, and books nothing more
    [Fact]
    public async Task Group_Full_Refused()
    {
        await using var db = await CreateSeededDbAsync();
        var token = await SendAsync(db);
        await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        var result = await Invites(db).AcceptAsync(token, PerId, new AcceptInviteRequest("91234568"));

        Assert.Equal(InviteOutcome.Full, result.Outcome);
        Assert.Equal(2, await db.Bookings.CountAsync());
    }

    // check if the boss can send the team without going: no visit for Kari, three places for the others
    [Fact]
    public async Task Group_OrganizerDoesNotJoin_OnlyTheOthersBooked()
    {
        await using var db = await CreateSeededDbAsync();
        var token = (await Invites(db).CreateAsync(Request(SofieId, persons: 3, organizerJoins: false,
            payMode: "JegSpanderer"), KariId)).Token!;

        await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));
        var view = await Invites(db).GetAsync(token, null);

        var booking = Assert.Single(db.Bookings);
        Assert.Equal(OlaId, booking.CustomerUserId);
        Assert.Equal(2, view!.PlacesLeft);
        Assert.Equal(["Ola"], view.Names);
        Assert.Null((await db.BookingInvites.SingleAsync()).EmployeeId); // Kari's choice was only for herself
    }

    // check if one "no" in a group only tells Kari, and the others can still join
    [Fact]
    public async Task Group_DeclineDoesNotCloseIt()
    {
        await using var db = await CreateSeededDbAsync();
        var token = (await Invites(db).CreateAsync(Request(persons: 3), KariId)).Token!;

        await Invites(db).DeclineAsync(token, PerId);
        var result = await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));

        Assert.Equal(InviteOutcome.Accepted, result.Outcome);
        Assert.Contains(db.Notifications, n => n.UserId == KariId && n.Text.StartsWith("Per kan dessverre"));
    }

    // check if the job closes a half-full group when the time is up, and the business gets the count once
    [Fact]
    public async Task CloseDue_HalfFullGroup_FinalCountOnce()
    {
        await using var db = await CreateSeededDbAsync();
        var mail = new FakeEmailService();
        var token = (await Invites(db).CreateAsync(Request(persons: 4), KariId)).Token!;
        await Invites(db).AcceptAsync(token, OlaId, new AcceptInviteRequest("91234567"));
        var invite = await db.BookingInvites.SingleAsync();
        invite.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var closed = await Invites(db, mail).CloseDueAsync(DateTime.UtcNow);
        var again = await Invites(db, mail).CloseDueAsync(DateTime.UtcNow);

        Assert.Equal(1, closed);
        Assert.Equal(0, again);
        Assert.Equal(InviteStatus.Akseptert, invite.Status); // the two booked keep their visits
        Assert.Single(db.Notifications, n => n.BusinessId == KlippOgStilId && n.Text.Contains("er klar: 2 personer"));
        var email = Assert.Single(mail.Sent, m => m.To == "eier@test.example");
        Assert.Contains("Kari Nordmann", email.Body);
        Assert.Contains("Ola Hansen", email.Body);
        Assert.Equal(InviteOutcome.Expired,
            (await Invites(db).AcceptAsync(token, PerId, new AcceptInviteRequest("91234568"))).Outcome);
    }

    // check if a group bigger than 8, or 1, is refused
    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public async Task Create_WrongGroupSize_Invalid(int persons)
    {
        await using var db = await CreateSeededDbAsync();

        var result = await Invites(db).CreateAsync(Request(persons: persons), KariId);

        Assert.Equal(InviteOutcome.Invalid, result.Outcome);
    }

    // check if a group bigger than the number of free employees is refused
    [Fact]
    public async Task Create_MorePeopleThanEmployees_SlotTaken()
    {
        await using var db = await CreateSeededDbAsync();

        var result = await Invites(db).CreateAsync(Request(persons: 5), KariId);

        Assert.Equal(InviteOutcome.SlotTaken, result.Outcome);
    }
}
