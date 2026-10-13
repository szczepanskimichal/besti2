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
        services.AddIdentityCore<AppUser>().AddEntityFrameworkStores<AppDbContext>();
        var provider = services.BuildServiceProvider();

        var db = provider.GetRequiredService<AppDbContext>();
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        return (new BusinessRegistrationService(db, users), db, users);
    }

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
    }

    [Fact]
    public async Task RegisterAsync_SameOrgNumberTwice_SecondFails()
    {
        var (service, _, _) = CreateService();

        await service.RegisterAsync(TestFrisor());
        var second = await service.RegisterAsync(TestFrisor());

        Assert.False(second.Success);
        Assert.Equal("Denne bedriften er allerede registrert.", second.Error);
    }
}