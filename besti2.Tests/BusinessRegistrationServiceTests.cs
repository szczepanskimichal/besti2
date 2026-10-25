using besti2.Application.Businesses;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Tests;

public class BusinessRegistrationServiceTests
{
    // Builds the service with a real UserManager on an in-memory database
    private static (BusinessRegistrationService Service, AppDbContext Db, UserManager<AppUser> Users) CreateService()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddIdentityCore<AppUser>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddTokenProvider<EmailTokenProvider<AppUser>>(TokenOptions.DefaultProvider);
        var provider = services.BuildServiceProvider();

        var db = provider.GetRequiredService<AppDbContext>();
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        return (new BusinessRegistrationService(db, users, new FakeGeocodingService(), new FakeEmailService(),
            "http://localhost:3000"), db, users);    }

    private static RegisterBusinessRequest TestFrisor() => new(
        "Test Frisør", "912345688", "Frisor", "eier@testfrisor.example",
        "+4790000001", "Testveien 1", "0150", "Oslo", "Test123!", AcceptDataProcessing: true);

    [Fact]
    public async Task RegisterAsync_ValidData_CreatesBusinessWithOwner()
    {
        var (service, db, users) = CreateService();

        var result = await service.RegisterAsync(TestFrisor());

        Assert.True(result.Success);
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345688");
        var owner = await users.FindByEmailAsync("eier@testfrisor.example");
        Assert.NotNull(owner);
        Assert.Equal(business.Id, owner.BusinessId);
        Assert.Equal(59.91, business.Latitude);
        Assert.Equal(10.75, business.Longitude);
        Assert.Single(db.Employees.Where(e => e.BusinessId == business.Id));
        Assert.Equal(5, db.OpeningHours.Count(h => h.BusinessId == business.Id));
    }
    
    [Fact]
    public async Task RegisterAsync_EmptyBusinessName_Fails()
    {
        var (service, _, _) = CreateService();

        var result = await service.RegisterAsync(TestFrisor() with { BusinessName = "" });

        Assert.False(result.Success);
        Assert.Equal("Fyll ut alle feltene.", result.Error);
    }
    
    // check if a weak password is refused before anything is saved (no half-registered business)
    [Fact]
    public async Task RegisterAsync_WeakPassword_SavesNothing()
    {
        var (service, db, users) = CreateService();

        var result = await service.RegisterAsync(TestFrisor() with { Password = "abc" });

        Assert.False(result.Success);
        Assert.StartsWith("Passordet må ha minst 6 tegn", result.Error);
        Assert.Empty(db.Businesses.Where(b => b.OrgNumber == "912345688"));
        Assert.Empty(users.Users);
    }
    
    // check if the owner must confirm the email: the account starts unconfirmed and a link is mailed
    [Fact]
    public async Task RegisterAsync_OwnerGetsConfirmationLink()
    {
        var (_, db, users) = CreateService();
        var email = new FakeEmailService();
        var service = new BusinessRegistrationService(db, users, new FakeGeocodingService(), email, "http://localhost:3000");

        var result = await service.RegisterAsync(TestFrisor());

        Assert.True(result.Success);
        var owner = await users.FindByEmailAsync("eier@testfrisor.example");
        Assert.False(owner!.EmailConfirmed);
        var mail = Assert.Single(email.Sent);
        Assert.Equal("eier@testfrisor.example", mail.To);
        Assert.Contains("http://localhost:3000/bekreft-epost?id=", mail.Body);
    }
    
    
    // check if a new business stays hidden until the owner confirms the email AND BESTI approves it
    [Fact]
    public async Task NewBusiness_IsHiddenUntilConfirmedAndApproved()
    {
        var (_, db, users) = CreateService();
        var email = new FakeEmailService();
        var service = new BusinessRegistrationService(db, users, new FakeGeocodingService(), email,
            "http://localhost:3000", adminEmail: "post@bestiapp.no");
        await service.RegisterAsync(TestFrisor());
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345688");
        Assert.True(business.IsHidden);
        Assert.NotNull(business.DataProcessingAcceptedUtc);

        // Not confirmed yet: stays hidden, BESTI gets nothing
        var owner = (await users.FindByEmailAsync("eier@testfrisor.example"))!;
        await service.ShowBusinessOfOwnerAsync(owner.Id);
        Assert.True(business.IsHidden);
        Assert.DoesNotContain(email.Sent, m => m.To == "post@bestiapp.no");

        // Confirmed: still hidden, BESTI gets the approval link
        await users.ConfirmEmailAsync(owner, await users.GenerateEmailConfirmationTokenAsync(owner));
        await service.ShowBusinessOfOwnerAsync(owner.Id);
        Assert.True(business.IsHidden);
        var request = Assert.Single(email.Sent, m => m.To == "post@bestiapp.no");
        var token = request.Body.Split("/godkjenn?token=")[1].Trim();

        // Approved: visible, the owner gets a mail, and the link does not work twice
        Assert.Equal("912345688", (await service.GetApprovalAsync(token))!.OrgNumber);
        Assert.True(await service.ApproveAsync(token));
        Assert.False(business.IsHidden);
        Assert.NotNull(business.ApprovedUtc);
        Assert.Contains(email.Sent, m => m.To == "eier@testfrisor.example" && m.Subject.Contains("godkjent"));
        Assert.False(await service.ApproveAsync(token));
        Assert.Null(await service.GetApprovalAsync(token));
    }

    // check if a wrong approval link does nothing
    [Fact]
    public async Task ApproveAsync_WrongToken_DoesNothing()
    {
        var (service, _, _) = CreateService();

        Assert.False(await service.ApproveAsync("feil"));
        Assert.Null(await service.GetApprovalAsync(""));
    }

    // check if "Avvis" deletes a stranger's registration with what they added in the panel,
    // mails them, and the real owner can then register the same org number
    [Fact]
    public async Task RejectAsync_DeletesRegistrationAndFreesOrgNumber()
    {
        var (_, db, users) = CreateService();
        var email = new FakeEmailService();
        var service = new BusinessRegistrationService(db, users, new FakeGeocodingService(), email,
            "http://localhost:3000", adminEmail: "post@bestiapp.no");
        await service.RegisterAsync(TestFrisor() with { Email = "fremmed@test.example" });
        var stranger = (await users.FindByEmailAsync("fremmed@test.example"))!;
        await users.ConfirmEmailAsync(stranger, await users.GenerateEmailConfirmationTokenAsync(stranger));
        await service.ShowBusinessOfOwnerAsync(stranger.Id);
        var token = email.Sent.Single(m => m.To == "post@bestiapp.no").Body.Split("/godkjenn?token=")[1].Trim();

        // The stranger already made a manual booking in the panel
        var businessId = stranger.BusinessId!.Value;
        var haircut = new Service { Id = Guid.NewGuid(), Name = "Klipp", DurationMinutes = 30, BusinessId = businessId };
        db.Services.Add(haircut);
        var bookingId = Guid.NewGuid();
        db.Bookings.Add(new Booking
        {
            Id = bookingId, StartUtc = DateTime.UtcNow.AddDays(1), EndUtc = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            CustomerName = "Kunde", CustomerPhone = "+4790000002", CustomerEmail = "kunde@test.example",
            Status = BookingStatus.Bekreftet, ServiceId = haircut.Id,
            EmployeeId = db.Employees.Single(e => e.BusinessId == businessId).Id
        });
        await db.SaveChangesAsync();

        Assert.True(await service.RejectAsync(token));
        Assert.Empty(db.Businesses.Where(b => b.OrgNumber == "912345688"));
        Assert.Empty(db.Bookings.Where(b => b.Id == bookingId));
        Assert.Null(await users.FindByEmailAsync("fremmed@test.example"));
        Assert.Contains(email.Sent, m => m.To == "fremmed@test.example" && m.Subject.Contains("avvist"));
        Assert.False(await service.RejectAsync(token));

        var result = await service.RegisterAsync(TestFrisor());
        Assert.True(result.Success);
    }

    // check if a wrong link deletes nothing, and an approved business can never be rejected
    [Fact]
    public async Task RejectAsync_WrongTokenOrApproved_DoesNothing()
    {
        var (_, db, users) = CreateService();
        var email = new FakeEmailService();
        var service = new BusinessRegistrationService(db, users, new FakeGeocodingService(), email,
            "http://localhost:3000", adminEmail: "post@bestiapp.no");
        await service.RegisterAsync(TestFrisor());
        var owner = (await users.FindByEmailAsync("eier@testfrisor.example"))!;
        await users.ConfirmEmailAsync(owner, await users.GenerateEmailConfirmationTokenAsync(owner));
        await service.ShowBusinessOfOwnerAsync(owner.Id);
        var token = email.Sent.Single(m => m.To == "post@bestiapp.no").Body.Split("/godkjenn?token=")[1].Trim();

        Assert.False(await service.RejectAsync("feil"));
        Assert.True(await service.ApproveAsync(token));
        Assert.False(await service.RejectAsync(token));
        Assert.Single(db.Businesses.Where(b => b.OrgNumber == "912345688"));
        Assert.NotNull(await users.FindByEmailAsync("eier@testfrisor.example"));
    }

    // check if the data processing agreement must be accepted, and nothing is saved without it
    [Fact]
    public async Task RegisterAsync_WithoutDataProcessingAgreement_Fails()
    {
        var (service, db, _) = CreateService();

        var result = await service.RegisterAsync(TestFrisor() with { AcceptDataProcessing = false });

        Assert.False(result.Success);
        Assert.Equal("Du må godta databehandleravtalen for å registrere bedriften.", result.Error);
        Assert.Empty(db.Businesses.Where(b => b.OrgNumber == "912345688"));
    }
    // check if an unconfirmed registration (lost mail, or someone else using the org number) does not
    // block a new registration: the old hidden business and its owner are replaced
    [Fact]
    public async Task RegisterAsync_SameOrgNumberUnconfirmed_ReplacesTheOldRegistration()
    {
        var (service, db, users) = CreateService();
        await service.RegisterAsync(TestFrisor() with { Email = "fremmed@test.example" });

        var result = await service.RegisterAsync(TestFrisor());

        Assert.True(result.Success);
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345688");
        var owner = await users.FindByEmailAsync("eier@testfrisor.example");
        Assert.Equal(business.Id, owner!.BusinessId);
        Assert.Null(await users.FindByEmailAsync("fremmed@test.example"));
    }

    // check if a CONFIRMED business can never be replaced by a new registration
    [Fact]
    public async Task RegisterAsync_SameOrgNumberConfirmed_StillFails()
    {
        var (service, _, users) = CreateService();
        await service.RegisterAsync(TestFrisor());
        var owner = (await users.FindByEmailAsync("eier@testfrisor.example"))!;
        await users.ConfirmEmailAsync(owner, await users.GenerateEmailConfirmationTokenAsync(owner));

        var result = await service.RegisterAsync(TestFrisor() with { Email = "fremmed@test.example" });

        Assert.False(result.Success);
        Assert.Equal("Denne bedriften er allerede registrert.", result.Error);
    }
    // check if the login e-mail stays private: no contact e-mail = nothing public, the same one is refused
    [Fact]
    public async Task RegisterAsync_LoginEmailIsNeverPublic()
    {
        var (service, db, _) = CreateService();

        var same = await service.RegisterAsync(TestFrisor() with { ContactEmail = " Eier@TestFrisor.example " });
        var empty = await service.RegisterAsync(TestFrisor());

        Assert.False(same.Success);
        Assert.StartsWith("Bruk en annen e-post til kunder", same.Error);
        Assert.True(empty.Success);
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345688");
        Assert.Null(business.Email);
    }
}
