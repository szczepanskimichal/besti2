using besti2.Application.Businesses;
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
        "Test Frisør", "912345678", "Frisor", "eier@testfrisor.example",
        "+4790000001", "Testveien 1", "0150", "Oslo", "Test123!");

    [Fact]
    public async Task RegisterAsync_ValidData_CreatesBusinessWithOwner()
    {
        var (service, db, users) = CreateService();

        var result = await service.RegisterAsync(TestFrisor());

        Assert.True(result.Success);
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345678");
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
        Assert.Empty(db.Businesses.Where(b => b.OrgNumber == "912345678"));
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
    
    
    // check if a new business is hidden until the owner confirms the email
    [Fact]
    public async Task NewBusiness_IsHiddenUntilOwnerConfirms()
    {
        var (service, db, users) = CreateService();
        await service.RegisterAsync(TestFrisor());
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345678");
        Assert.True(business.IsHidden);

        // Not confirmed yet: stays hidden
        var owner = (await users.FindByEmailAsync("eier@testfrisor.example"))!;
        await service.ShowBusinessOfOwnerAsync(owner.Id);
        Assert.True(business.IsHidden);

        // Confirmed: visible
        await users.ConfirmEmailAsync(owner, await users.GenerateEmailConfirmationTokenAsync(owner));
        await service.ShowBusinessOfOwnerAsync(owner.Id);
        Assert.False(business.IsHidden);
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
        var business = await db.Businesses.SingleAsync(b => b.OrgNumber == "912345678");
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
}