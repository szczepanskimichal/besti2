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

public class BusinessSlugTests
{
    [Theory]
    [InlineData("Klipp & Stil Frisør", "Gjøvik", "klipp-og-stil-frisor-gjovik")]
    [InlineData("Salong Kari", "Gjøvik", "salong-kari-gjovik")]
    [InlineData("  Hår.Studio (Søndre)  ", "Ålesund", "har-studio-sondre-alesund")]
    [InlineData("Café Bæ", "Tromsø", "cafe-bae-tromso")]
    [InlineData("!!!", "", "bedrift")]
    public void From_MakesShortAddress(string name, string city, string expected)
    {
        Assert.Equal(expected, BusinessSlug.From(name, city));
    }

    [Fact]
    public void From_CutsLongNames()
    {
        var slug = BusinessSlug.From(new string('a', 70) + " frisør", "Gjøvik");

        Assert.True(slug.Length <= BusinessSlug.MaxLength);
        Assert.True(BusinessSlug.IsValid(slug));
    }

    [Theory]
    [InlineData("salong-kari-gjovik", true)]
    [InlineData("Salong-Kari", false)]
    [InlineData("salong--kari", false)]
    [InlineData("-salong", false)]
    [InlineData("salong kari", false)]
    [InlineData("", false)]
    public void IsValid_OnlyAcceptsSlugs(string slug, bool expected)
    {
        Assert.Equal(expected, BusinessSlug.IsValid(slug));
    }

    [Fact]
    public void IsReserved_BlocksPagesOfTheWebsite()
    {
        Assert.True(BusinessSlug.IsReserved("panel"));
        Assert.False(BusinessSlug.IsReserved("salong-kari-gjovik"));
    }

    [Fact]
    public async Task GetBySlugAsync_FindsVisibleBusiness()
    {
        await using var db = await CreateDbAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        var result = await service.GetBySlugAsync("klipp-og-stil-frisor-oslo");

        Assert.NotNull(result);
        Assert.Equal("Klipp & Stil Frisør", result.Name);
        Assert.Equal("klipp-og-stil-frisor-oslo", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_HiddenOrUnknown_ReturnsNull()
    {
        await using var db = await CreateDbAsync();
        var klipp = await db.Businesses.FirstAsync(b => b.Slug == "klipp-og-stil-frisor-oslo");
        klipp.IsHidden = true;
        await db.SaveChangesAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        Assert.Null(await service.GetBySlugAsync("klipp-og-stil-frisor-oslo"));
        Assert.Null(await service.GetBySlugAsync("finnes-ikke"));
        Assert.Null(await service.GetBySlugAsync("../panel"));
    }

    [Fact]
    public async Task RegisterAsync_GivesNextNumber_WhenAddressIsTaken()
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
        db.Businesses.Add(new Business
        {
            Id = Guid.NewGuid(), Name = "Test Frisør", Slug = "test-frisor-oslo", OrgNumber = "999000009",
            Category = BusinessCategory.Frisor, PhoneNumber = "+4790000002", Address = "Gata 2", PostalCode = "0150",
            City = "Oslo"
        });
        await db.SaveChangesAsync();
        var registration = new BusinessRegistrationService(db, provider.GetRequiredService<UserManager<AppUser>>(),
            new FakeGeocodingService(), new FakeEmailService(), "http://localhost:3000");

        var result = await registration.RegisterAsync(new RegisterBusinessRequest(
            "Test Frisør", "912345688", "Frisor", "eier@testfrisor.example",
            "+4790000001", "Testveien 1", "0150", "Oslo", "Test123!", AcceptDataProcessing: true));

        Assert.True(result.Success);
        Assert.Equal("test-frisor-oslo-2", (await db.Businesses.SingleAsync(b => b.OrgNumber == "912345688")).Slug);
    }

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
