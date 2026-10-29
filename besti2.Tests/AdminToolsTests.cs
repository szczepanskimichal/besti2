using besti2.Application.Reviews;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// /admin tools: hiding reviews, closing a business, finding a customer for support, the weeks and the log
public class AdminToolsTests
{
    private static readonly DateTime Now = new(2030, 1, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private const string Ada = "ada@test.example";

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Booking NewBooking(DateTime startUtc, string? userId = "kunde-1", string phone = "+4791234567",
        BookingStatus status = BookingStatus.Bekreftet) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = startUtc.AddDays(-3),
        CustomerName = "Kari Nordmann",
        CustomerPhone = phone,
        CustomerEmail = "kari@test.example",
        CustomerUserId = userId,
        Status = status,
        ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
        EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001")
    };

    private static async Task<Review> AddReviewAsync(AppDbContext db, int stars)
    {
        var booking = NewBooking(Now.AddDays(-2));
        var review = new Review
        {
            Id = Guid.NewGuid(), BookingId = booking.Id, BusinessId = KlippOgStil, CustomerUserId = "kunde-1",
            CustomerName = "Kari", Stars = stars, Comment = "Tekst", CreatedUtc = Now.AddDays(-1)
        };
        db.Bookings.Add(booking);
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        return review;
    }

    // check if a hidden review leaves the page and the stars, both sides are told, and it can be shown again
    [Fact]
    public async Task HideReview_NotShownOrCountedAndBothAreTold()
    {
        await using var db = await CreateSeededDbAsync();
        var good = await AddReviewAsync(db, 5);
        var bad = await AddReviewAsync(db, 1);
        var admin = new AdminService(db);
        var reviews = new ReviewService(db);

        Assert.NotNull(await admin.HideReviewAsync(bad.Id, "Fordi jeg vil", Ada));
        Assert.Null(await admin.HideReviewAsync(bad.Id, AdminService.ReviewReasons[0], Ada));
        var page = await reviews.GetForBusinessAsync(KlippOgStil);
        var panel = await reviews.GetForBusinessAsync(KlippOgStil, includeHidden: true);

        Assert.Equal(5, page.Average);
        Assert.Equal(good.Id, Assert.Single(page.Reviews).Id);
        Assert.Equal(2, panel.Reviews.Count);
        Assert.Equal(1, panel.Count);
        Assert.Contains(panel.Reviews, r => r.Hidden && r.HiddenReason == AdminService.ReviewReasons[0]);
        Assert.Contains(db.Notifications, n => n.UserId == "kunde-1" && n.Text.Contains("skjult vurderingen din"));
        Assert.Contains(db.Notifications, n => n.BusinessId == KlippOgStil && n.Text.Contains("skjult en vurdering"));
        Assert.Contains(await admin.GetLogAsync(10), a => a.AdminEmail == Ada && a.Text.StartsWith("Skjulte"));

        Assert.Null(await admin.ShowReviewAsync(bad.Id, Ada));
        Assert.Equal(3, (await reviews.GetForBusinessAsync(KlippOgStil)).Average);
    }

    // check if a closed business is hidden with the reason, the owner gets an e-mail, and opening it again works
    [Fact]
    public async Task SuspendBusiness_HiddenUntilReopened()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            UserName = "eier@test.example", Email = "eier@test.example", BusinessId = KlippOgStil,
            EmailConfirmed = true
        });
        await db.SaveChangesAsync();
        var email = new FakeEmailService();
        var admin = new AdminService(db, email);

        Assert.NotNull(await admin.SuspendBusinessAsync(KlippOgStil, null, Ada));
        Assert.Null(await admin.SuspendBusinessAsync(KlippOgStil, AdminService.SuspendReasons[1], Ada));
        var business = await db.Businesses.SingleAsync(b => b.Id == KlippOgStil);
        var row = (await admin.GetBusinessesAsync("Stengt", null, Now)).Single();

        Assert.True(business.IsHidden);
        Assert.Equal(AdminService.SuspendReasons[1], business.SuspendedReason);
        Assert.Equal(KlippOgStil, row.Id);
        Assert.Contains(email.Sent, m => m.To == "eier@test.example" && m.Body.Contains("Flere klager"));
        Assert.NotNull(await admin.SuspendBusinessAsync(KlippOgStil, AdminService.SuspendReasons[1], Ada));

        Assert.Null(await admin.ReopenBusinessAsync(KlippOgStil, Ada));
        Assert.False((await db.Businesses.SingleAsync(b => b.Id == KlippOgStil)).IsHidden);
        Assert.Equal(2, (await admin.GetBusinessAsync(KlippOgStil, Now))!.Log!.Count);
    }

    // check if a customer is found only by the exact e-mail or the phone number, never by a part of it
    [Fact]
    public async Task FindCustomers_OnlyExactEmailOrPhone()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser
        {
            Id = "kunde-1", UserName = "kari@test.example", Email = "kari@test.example",
            NormalizedEmail = "KARI@TEST.EXAMPLE", PhoneNumber = "912 34 567", FullName = "Kari Nordmann"
        });
        db.Users.Add(new AppUser
        {
            UserName = "eier@test.example", Email = "eier@test.example", NormalizedEmail = "EIER@TEST.EXAMPLE",
            BusinessId = KlippOgStil
        });
        db.Bookings.Add(NewBooking(Now.AddDays(2)));
        await db.SaveChangesAsync();
        var admin = new AdminService(db);

        var byEmail = Assert.Single(await admin.FindCustomersAsync("Kari@Test.example", Now));
        var byPhone = Assert.Single(await admin.FindCustomersAsync("+47 91234567", Now));

        Assert.Empty(await admin.FindCustomersAsync("kari", Now));
        Assert.Empty(await admin.FindCustomersAsync("eier@test.example", Now));
        Assert.Equal("kunde-1", byPhone.Id);
        Assert.False(byEmail.EmailConfirmed);
        Assert.Equal("Herreklipp", Assert.Single(byEmail.Upcoming).ServiceName);
    }

    // check if "Utvikling" counts new businesses, customers and bookings in the right Oslo week
    [Fact]
    public async Task Overview_CountsWeeks()
    {
        await using var db = await CreateSeededDbAsync();
        db.Users.Add(new AppUser { UserName = "a@test.example", Email = "a@test.example", CreatedUtc = Now.AddDays(-1) });
        db.Users.Add(new AppUser { UserName = "b@test.example", Email = "b@test.example", CreatedUtc = Now.AddDays(-8) });
        db.Bookings.Add(NewBooking(Now.AddDays(3)));
        await db.SaveChangesAsync();

        var overview = await new AdminService(db).GetOverviewAsync(Now);

        Assert.Equal(AdminService.GrowthWeeks, overview.Weeks.Count);
        // Wednesday 9 January 2030: this week starts Monday the 7th
        Assert.Equal(new DateOnly(2030, 1, 7), overview.Weeks[^1].WeekStart);
        Assert.Equal(1, overview.Weeks[^1].NewCustomers);
        Assert.Equal(1, overview.Weeks[^2].NewCustomers);
        Assert.Equal(1, overview.Weeks[^1].Bookings);
    }
}
