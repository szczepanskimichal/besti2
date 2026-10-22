using besti2.Application.Businesses;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Tests;

public class BrregTests
{
    private static (BusinessRegistrationService Service, AppDbContext Db) Create(FakeBusinessRegistry registry)
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
            "http://localhost:3000", registry), db);
    }

    private static RegisterBusinessRequest Frisor(string orgNumber = "912345688") => new(
        "Test Frisør", orgNumber, "Frisor", "eier@testfrisor.example",
        "+4790000001", "Testveien 1", "0150", "Oslo", "Test123!");

    // check if the check digit (modulus 11) catches typos
    [Theory]
    [InlineData("923609016", true)]
    [InlineData("912345688", true)]
    [InlineData("912345678", false)]
    [InlineData("92360901a", false)]
    [InlineData("12345678", false)]
    public void OrgNumber_CheckDigit(string orgNumber, bool valid)
    {
        Assert.Equal(valid, OrgNumber.IsValid(orgNumber));
    }

    // check if a number with a wrong check digit is refused before anything else
    [Fact]
    public async Task Register_WrongCheckDigit_IsRefused()
    {
        var (service, _) = Create(new FakeBusinessRegistry());

        var result = await service.RegisterAsync(Frisor("912345678"));

        Assert.False(result.Success);
        Assert.Contains("Ugyldig organisasjonsnummer", result.Error);
    }

    // check if a business that is not in the register, or is closed, cannot sign up
    [Theory]
    [InlineData(RegistryStatus.NotFound, "Fant ikke virksomheten")]
    [InlineData(RegistryStatus.Closed, "slettet, konkurs eller under avvikling")]
    public async Task Register_NotActiveInTheRegister_IsRefused(RegistryStatus status, string message)
    {
        var (service, db) = Create(new FakeBusinessRegistry { Result = new RegistryLookup(status) });

        var result = await service.RegisterAsync(Frisor());

        Assert.False(result.Success);
        Assert.Contains(message, result.Error);
        Assert.Empty(db.Businesses.Where(b => b.OrgNumber == "912345688"));
    }

    // check if the registration goes on when the register does not answer
    [Fact]
    public async Task Register_RegisterDown_StillWorks()
    {
        var (service, _) = Create(new FakeBusinessRegistry { Result = new RegistryLookup(RegistryStatus.Unavailable) });

        var result = await service.RegisterAsync(Frisor());

        Assert.True(result.Success);
    }
}
