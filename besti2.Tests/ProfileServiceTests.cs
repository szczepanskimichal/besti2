using besti2.Application.Accounts;
using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

public class ProfileServiceTests
{
    // Monday 5 Jan 2099 (Klipp & Stil is open 09–17); far in the future, so the test never gets old
    private static readonly DateOnly FutureMonday = new(2099, 1, 5);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HerreklippId = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid IngridId = new("b1111111-0000-0000-0000-000000000001");
    private const string Password = "Test123!";

    // Real UserManager and the same in-memory database for bookings, reviews and notifications
    private static async Task<(ProfileService Profile, UserManager<AppUser> Users, AppDbContext Db, BookingService Bookings)>
        CreateAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddIdentityCore<AppUser>().AddEntityFrameworkStores<AppDbContext>();
        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var bookings = new BookingService(db, new FakeEmailService(), NullLogger<BookingService>.Instance);
        return (new ProfileService(db, users, bookings), users, db, bookings);
    }

    private static async Task<AppUser> KariAsync(UserManager<AppUser> users, Guid? businessId = null)
    {
        var user = new AppUser
        {
            UserName = "kari@test.example", Email = "kari@test.example", FullName = "Kari Nordmann",
            EmailConfirmed = true, BusinessId = businessId
        };
        await users.CreateAsync(user, Password);
        return user;
    }

    // check if name and phone are saved, and a wrong phone is refused
    [Fact]
    public async Task Update_SavesNameAndPhone()
    {
        var (profile, users, _, _) = await CreateAsync();
        var kari = await KariAsync(users);

        var wrong = await profile.UpdateAsync(kari.Id, new UpdateMyProfileRequest("Kari N.", "abc"));
        var ok = await profile.UpdateAsync(kari.Id, new UpdateMyProfileRequest("  Kari Hansen ", "912 34 567"));

        var shown = await profile.GetAsync(kari.Id);
        Assert.NotNull(wrong);
        Assert.Null(ok);
        Assert.Equal("Kari Hansen", shown!.FullName);
        Assert.Equal("912 34 567", shown.Phone);
        Assert.Equal("kari@test.example", shown.Email);
    }

    // check if the password changes only with the right current password
    [Fact]
    public async Task ChangePassword_NeedsTheCurrentOne()
    {
        var (profile, users, _, _) = await CreateAsync();
        var kari = await KariAsync(users);

        var wrong = await profile.ChangePasswordAsync(kari.Id, new ChangePasswordRequest("feil-passord", "Nytt123!"));
        var ok = await profile.ChangePasswordAsync(kari.Id, new ChangePasswordRequest(Password, "Nytt123!"));

        Assert.Equal("Nåværende passord er feil.", wrong);
        Assert.Null(ok);
        Assert.True(await users.CheckPasswordAsync((await users.FindByIdAsync(kari.Id))!, "Nytt123!"));
    }

    // check if deleting cancels coming visits (the business is told), anonymises visits, keeps reviews as
    // "Tidligere kunde", removes the bell and the account
    [Fact]
    public async Task Delete_CleansEverything()
    {
        var (profile, users, db, bookings) = await CreateAsync();
        var kari = await KariAsync(users);
        var coming = await bookings.CreateAsync(new CreateBookingRequest(HerreklippId, IngridId,
            SlotCalculator.ToUtc(FutureMonday, new TimeOnly(10, 0)), "Kari Nordmann", "+4790000000",
            "kari@test.example", false), kari.Id);
        var past = new Booking
        {
            Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddDays(-3), EndUtc = DateTime.UtcNow.AddDays(-3).AddMinutes(30),
            CustomerName = "Kari Nordmann", CustomerPhone = "90000000", CustomerEmail = "kari@test.example",
            Status = BookingStatus.Bekreftet, CustomerUserId = kari.Id, EmployeeId = IngridId, ServiceId = HerreklippId
        };
        db.Bookings.Add(past);
        db.Reviews.Add(new Review
        {
            Id = Guid.NewGuid(), BookingId = past.Id, BusinessId = KlippOgStil, CustomerUserId = kari.Id,
            CustomerName = "Kari", Stars = 5, Comment = "Supert!", CreatedUtc = DateTime.UtcNow
        });
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(), UserId = kari.Id, Text = "Hei", CreatedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var wrong = await profile.DeleteAsync(kari.Id, new DeleteAccountRequest("feil"));
        var ok = await profile.DeleteAsync(kari.Id, new DeleteAccountRequest(Password));

        Assert.Equal("Passordet er feil.", wrong);
        Assert.Null(ok);
        Assert.Null(await users.FindByIdAsync(kari.Id));
        var cancelled = await db.Bookings.SingleAsync(b => b.Id == coming.Booking!.Id);
        Assert.Equal(BookingStatus.Avlyst, cancelled.Status);
        Assert.All(await db.Bookings.ToListAsync(), b =>
        {
            Assert.Equal(ProfileService.DeletedCustomer, b.CustomerName);
            Assert.Null(b.CustomerUserId);
            Assert.Equal(BookingAnonymizer.AnonymizedEmail, b.CustomerEmail);
        });
        var review = await db.Reviews.SingleAsync();
        Assert.Equal(ProfileService.FormerCustomer, review.CustomerName);
        Assert.Equal("Supert!", review.Comment);
        Assert.Empty(await db.Notifications.Where(n => n.UserId == kari.Id).ToListAsync());
        Assert.Contains(await db.Notifications.Where(n => n.BusinessId == KlippOgStil).ToListAsync(),
            n => n.Text.Contains("har avbestilt"));
    }

    // check if a business owner cannot use the customer's profile (and cannot delete the business this way)
    [Fact]
    public async Task BusinessOwner_IsNotACustomer()
    {
        var (profile, users, _, _) = await CreateAsync();
        var owner = await KariAsync(users, KlippOgStil);

        Assert.Null(await profile.GetAsync(owner.Id));
        Assert.Equal("Fant ikke kontoen.", await profile.DeleteAsync(owner.Id, new DeleteAccountRequest(Password)));
        Assert.NotNull(await users.FindByIdAsync(owner.Id));
    }
}
