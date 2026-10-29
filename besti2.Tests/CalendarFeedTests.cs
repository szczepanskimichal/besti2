using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Tests;

// "Kalender på telefonen": the business's bookings as a calendar Google / Apple Calendar subscribes to
public class CalendarFeedTests
{
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");

    private static Booking NewBooking(DateTime startUtc, BookingStatus status, string name) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = Now.AddDays(-3),
        CustomerName = name,
        CustomerPhone = "+4790000000",
        CustomerEmail = "kunde@test.example",
        CustomerNote = "Kort, takk; ikke for kort",
        Status = status,
        ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
        EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001")
    };

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    // check if the calendar has the coming visits, marks a waiting one, and leaves out cancelled and old ones
    [Fact]
    public async Task GetCalendarAsync_HasVisitsWithoutCancelledOrOld()
    {
        await using var db = await CreateSeededDbAsync();
        var confirmed = NewBooking(Now.AddDays(1), BookingStatus.Bekreftet, "Kari Nordmann");
        db.Bookings.AddRange(
            confirmed,
            NewBooking(Now.AddDays(2), BookingStatus.Venter, "Ola Venter"),
            NewBooking(Now.AddDays(3), BookingStatus.Avlyst, "Per Avlyst"),
            NewBooking(Now.AddDays(-60), BookingStatus.Bekreftet, "Gammel Kunde"));
        await db.SaveChangesAsync();
        var service = new CalendarFeedService(db);
        var token = await service.CreateTokenAsync(KlippOgStil);

        var ics = await service.GetCalendarAsync(token, Now);

        Assert.NotNull(ics);
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains($"UID:{confirmed.Id}@bestiapp.no", ics);
        Assert.Contains("DTSTART:20300108T090000Z", ics);
        Assert.Contains("Kari Nordmann", ics);
        Assert.Contains("Venter: ", ics);
        Assert.Contains("STATUS:TENTATIVE", ics);
        // Commas and semicolons are escaped
        Assert.Contains(@"Kort\, takk\; ikke for kort", ics.Replace("\r\n ", ""));
        Assert.DoesNotContain("Per Avlyst", ics);
        Assert.DoesNotContain("Gammel Kunde", ics);
        Assert.Equal(2, ics.Split("BEGIN:VEVENT").Length - 1);
        // No line longer than 75 bytes (longer ones are folded)
        Assert.All(ics.Split("\r\n"), line => Assert.True(System.Text.Encoding.UTF8.GetByteCount(line) <= 75));
    }

    // check if a new address replaces the old one, and turning it off makes it stop working
    [Fact]
    public async Task CreateTokenAsync_NewAddressStopsOldOne()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new CalendarFeedService(db);

        var first = await service.CreateTokenAsync(KlippOgStil);
        var second = await service.CreateTokenAsync(KlippOgStil);

        Assert.Equal(64, second.Length);
        Assert.NotEqual(first, second);
        Assert.Null(await service.GetCalendarAsync(first, Now));
        Assert.NotNull(await service.GetCalendarAsync(second, Now));

        await service.TurnOffAsync(KlippOgStil);

        Assert.Null(await service.GetTokenAsync(KlippOgStil));
        Assert.Null(await service.GetCalendarAsync(second, Now));
        Assert.Null(await service.GetCalendarAsync("", Now));
    }

    // check if the owner turns it on in the panel and the calendar address works without a login
    [Fact]
    public async Task CalendarFeed_OwnerTurnsOnAndAddressWorksWithoutLogin()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var owner = new AppUser
            {
                UserName = "eier@test.example", Email = "eier@test.example", EmailConfirmed = true,
                BusinessId = KlippOgStil
            };
            Assert.True((await users.CreateAsync(owner, "Test123!")).Succeeded);
        }
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "eier@test.example", password = "Test123!" });
        var loginToken = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginToken);

        var created = await client.PostAsync("/api/panel/calendar-feed", null);
        var feedToken = (await created.Content.ReadFromJsonAsync<FeedToken>())!.Token!;
        client.DefaultRequestHeaders.Authorization = null;
        var calendar = await client.GetAsync($"/api/calendar/{feedToken}");
        var wrong = await client.GetAsync("/api/calendar/feil");

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        Assert.Equal("text/calendar", calendar.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
    }

    private sealed record TokenResponse(string AccessToken);

    private sealed record FeedToken(string? Token);
}
