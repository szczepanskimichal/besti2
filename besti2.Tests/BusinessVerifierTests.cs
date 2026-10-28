using besti2.Application.Businesses;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// "Verifisert bedrift": the weekly check in Brønnøysund
public class BusinessVerifierTests
{
    private static readonly Guid KlippId = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Now = new(2026, 11, 2, 3, 0, 0, DateTimeKind.Utc);

    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static BusinessVerifier CreateVerifier(AppDbContext db, FakeBusinessRegistry registry,
        FakeEmailService email) =>
        new(db, registry, email, NullLogger<BusinessVerifier>.Instance, "admin@besti.example");

    // check if an approved business that was never checked gets the badge
    [Fact]
    public async Task RecheckAsync_ActiveBusiness_GetsBadge()
    {
        await using var db = await CreateDbAsync();
        var klipp = await db.Businesses.FindAsync(KlippId);
        klipp!.ApprovedUtc = Now.AddDays(-30);
        await db.SaveChangesAsync();
        var email = new FakeEmailService();

        var lost = await CreateVerifier(db, new FakeBusinessRegistry(), email).RecheckAsync(Now);

        Assert.Equal(0, lost);
        Assert.Equal(Now, (await db.Businesses.FindAsync(KlippId))!.VerifiedUtc);
        Assert.Empty(email.Sent);
    }

    // check if a business checked a few days ago and a business never approved are not asked again
    [Fact]
    public async Task RecheckAsync_RecentlyCheckedOrNotApproved_IsSkipped()
    {
        await using var db = await CreateDbAsync();
        var klipp = await db.Businesses.FindAsync(KlippId);
        klipp!.ApprovedUtc = Now.AddDays(-30);
        klipp.VerifiedUtc = Now.AddDays(-3);
        await db.SaveChangesAsync();
        // Would remove every badge if anyone was asked
        var registry = new FakeBusinessRegistry { Result = new RegistryLookup(RegistryStatus.Closed) };

        await CreateVerifier(db, registry, new FakeEmailService()).RecheckAsync(Now);

        Assert.Equal(Now.AddDays(-3), (await db.Businesses.FindAsync(KlippId))!.VerifiedUtc);
        // The other two demo businesses are not approved: no badge
        Assert.Equal(1, await db.Businesses.CountAsync(b => b.VerifiedUtc != null));
    }

    // check if a business that went bankrupt loses the badge and BESTI gets one mail
    [Fact]
    public async Task RecheckAsync_ClosedBusiness_LosesBadgeAndAdminGetsMail()
    {
        await using var db = await CreateDbAsync();
        var klipp = await db.Businesses.FindAsync(KlippId);
        klipp!.ApprovedUtc = Now.AddDays(-30);
        klipp.VerifiedUtc = Now.AddDays(-8);
        await db.SaveChangesAsync();
        var registry = new FakeBusinessRegistry { Result = new RegistryLookup(RegistryStatus.Closed) };
        var email = new FakeEmailService();

        var lost = await CreateVerifier(db, registry, email).RecheckAsync(Now);

        Assert.Equal(1, lost);
        Assert.Null((await db.Businesses.FindAsync(KlippId))!.VerifiedUtc);
        var mail = Assert.Single(email.Sent);
        Assert.Equal("admin@besti.example", mail.To);
        Assert.Contains("Klipp & Stil Frisør (org.nr. 999000001)", mail.Body);

        // The next day: still closed, but no new mail
        await CreateVerifier(db, registry, email).RecheckAsync(Now.AddDays(1));
        Assert.Single(email.Sent);
    }

    // check if the badge stays when Brønnøysund does not answer
    [Fact]
    public async Task RecheckAsync_RegisterDown_KeepsBadge()
    {
        await using var db = await CreateDbAsync();
        var klipp = await db.Businesses.FindAsync(KlippId);
        klipp!.ApprovedUtc = Now.AddDays(-30);
        klipp.VerifiedUtc = Now.AddDays(-8);
        await db.SaveChangesAsync();
        var registry = new FakeBusinessRegistry { Result = new RegistryLookup(RegistryStatus.Unavailable) };

        var lost = await CreateVerifier(db, registry, new FakeEmailService()).RecheckAsync(Now);

        Assert.Equal(0, lost);
        Assert.Equal(Now.AddDays(-8), (await db.Businesses.FindAsync(KlippId))!.VerifiedUtc);
    }

    // check if customers only see the badge on an approved business
    [Fact]
    public async Task BusinessPage_ShowsVerifiedOnlyWhenApproved()
    {
        await using var db = await CreateDbAsync();
        var klipp = await db.Businesses.FindAsync(KlippId);
        klipp!.VerifiedUtc = Now;
        await db.SaveChangesAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var search = new BusinessSearchService(db, new FakeImageStorage());

        Assert.Null((await service.GetByIdAsync(KlippId))!.VerifiedUtc);
        Assert.False((await search.GetAllAsync()).Single(b => b.Id == KlippId).IsVerified);

        klipp.ApprovedUtc = Now;
        await db.SaveChangesAsync();

        Assert.Equal(Now, (await service.GetByIdAsync(KlippId))!.VerifiedUtc);
        Assert.True((await search.GetAllAsync()).Single(b => b.Id == KlippId).IsVerified);
    }
}
