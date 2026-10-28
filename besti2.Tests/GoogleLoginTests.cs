using besti2.Application.Accounts;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class GoogleLoginTests
{
    // Pretends to be Google: "ok" = a valid token for the identity, anything else = invalid
    private class FakeGoogle(GoogleIdentity identity) : IGoogleTokenValidator
    {
        public Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(idToken == "ok" ? identity : null);
    }

    private static readonly GoogleIdentity Kari = new("google-123", "Kari@Gmail.example", true, "Kari Nordmann");

    private static (UserManager<AppUser> Users, AppDbContext Db) CreateUsers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddIdentityCore<AppUser>().AddEntityFrameworkStores<AppDbContext>();
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<UserManager<AppUser>>(), provider.GetRequiredService<AppDbContext>());
    }

    // check if a new Google user gets a confirmed customer account without a password, and the same one next time
    [Fact]
    public async Task NewGoogleUser_GetsCustomerAccount()
    {
        var (users, _) = CreateUsers();
        var service = new GoogleLoginService(users, new FakeGoogle(Kari));

        var first = await service.FindOrCreateUserAsync("ok");
        var second = await service.FindOrCreateUserAsync("ok");

        var user = (await users.FindByIdAsync(first.UserId!))!;
        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal("kari@gmail.example", user.Email);
        Assert.Equal("Kari Nordmann", user.FullName);
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.BusinessId);
        Assert.False(await users.HasPasswordAsync(user));
    }

    // check if a wrong token or an e-mail Google has not checked never logs in
    [Fact]
    public async Task InvalidTokenOrUnverifiedEmail_Fails()
    {
        var (users, _) = CreateUsers();
        var unverified = new GoogleLoginService(users, new FakeGoogle(Kari with { EmailVerified = false }));

        Assert.NotNull((await new GoogleLoginService(users, new FakeGoogle(Kari)).FindOrCreateUserAsync("feil")).Error);
        Assert.NotNull((await unverified.FindOrCreateUserAsync("ok")).Error);
        Assert.Empty(await users.Users.ToListAsync());
    }

    // check if an existing confirmed account with the same e-mail is used and linked to Google
    [Fact]
    public async Task ExistingAccount_IsLinked()
    {
        var (users, _) = CreateUsers();
        var kari = new AppUser { UserName = "kari@gmail.example", Email = "kari@gmail.example", EmailConfirmed = true };
        await users.CreateAsync(kari, "Test123!");

        var result = await new GoogleLoginService(users, new FakeGoogle(Kari)).FindOrCreateUserAsync("ok");

        Assert.Equal(kari.Id, result.UserId);
        Assert.NotNull(await users.FindByLoginAsync("Google", "google-123"));
        Assert.True(await users.CheckPasswordAsync(kari, "Test123!"));
    }

    // check if an unconfirmed sign-up (maybe by a stranger) is confirmed WITHOUT keeping its password
    [Fact]
    public async Task UnconfirmedAccount_LosesStrangersPassword()
    {
        var (users, _) = CreateUsers();
        var stranger = new AppUser { UserName = "kari@gmail.example", Email = "kari@gmail.example" };
        await users.CreateAsync(stranger, "Stranger1!");

        var result = await new GoogleLoginService(users, new FakeGoogle(Kari)).FindOrCreateUserAsync("ok");

        var user = (await users.FindByIdAsync(result.UserId!))!;
        Assert.True(user.EmailConfirmed);
        Assert.False(await users.HasPasswordAsync(user));
    }

    // check if a business whose sign-up is not confirmed cannot skip it with Google
    [Fact]
    public async Task UnconfirmedBusiness_Fails()
    {
        var (users, _) = CreateUsers();
        await users.CreateAsync(new AppUser
        {
            UserName = "kari@gmail.example", Email = "kari@gmail.example", BusinessId = Guid.NewGuid()
        }, "Test123!");

        var result = await new GoogleLoginService(users, new FakeGoogle(Kari)).FindOrCreateUserAsync("ok");

        Assert.Null(result.UserId);
    }

    // check if a Google account without a password can make one, and delete itself by typing SLETT
    [Fact]
    public async Task GoogleAccount_CanMakePasswordAndDelete()
    {
        var (users, db) = CreateUsers();
        await db.Database.EnsureCreatedAsync();
        var userId = (await new GoogleLoginService(users, new FakeGoogle(Kari)).FindOrCreateUserAsync("ok")).UserId!;
        var profile = new ProfileService(db, users,
            new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance));

        Assert.False((await profile.GetAsync(userId))!.HasPassword);
        Assert.NotNull(await profile.DeleteAsync(userId, new DeleteAccountRequest("")));
        Assert.Null(await profile.ChangePasswordAsync(userId, new ChangePasswordRequest("", "Nytt123!")));
        Assert.True((await profile.GetAsync(userId))!.HasPassword);

        var (users2, db2) = CreateUsers();
        await db2.Database.EnsureCreatedAsync();
        var otherId = (await new GoogleLoginService(users2, new FakeGoogle(Kari)).FindOrCreateUserAsync("ok")).UserId!;
        var profile2 = new ProfileService(db2, users2,
            new BookingService(db2, new FakeEmailService(), NullLogger<BookingService>.Instance));
        Assert.Null(await profile2.DeleteAsync(otherId, new DeleteAccountRequest("slett")));
        Assert.Null(await users2.FindByIdAsync(otherId));
    }
}
