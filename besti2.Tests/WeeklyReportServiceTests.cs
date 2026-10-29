using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace besti2.Tests;

// "Ukerapport": Monday morning, last week's numbers to the owner, once a week
public class WeeklyReportServiceTests
{
    // Monday 7 January 2030, 08:00 in Oslo
    private static readonly DateTime MondayMorning = new(2030, 1, 7, 7, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");

    private static async Task<(AppDbContext Db, FakeEmailService Mail, WeeklyReportService Service)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        // Only Klipp & Stil is approved, with an owner account
        foreach (var business in db.Businesses)
        {
            business.ApprovedUtc = business.Id == KlippOgStil ? MondayMorning.AddDays(-30) : null;
        }
        db.Users.Add(new AppUser { UserName = "eier@test.example", Email = "eier@test.example", BusinessId = KlippOgStil });
        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(), StartUtc = MondayMorning.AddDays(-3), EndUtc = MondayMorning.AddDays(-3).AddMinutes(30),
            CreatedUtc = MondayMorning.AddDays(-5), CustomerName = "Kari", CustomerPhone = "+4790000000",
            CustomerEmail = "kari@test.example", CustomerUserId = "kari", Status = BookingStatus.Bekreftet,
            PriceNok = 7450m, ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
            EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001")
        });
        await db.SaveChangesAsync();
        var mail = new FakeEmailService();
        var service = new WeeklyReportService(db, new BusinessStatsService(db), mail,
            NullLogger<WeeklyReportService>.Instance, "https://bestiapp.no");
        return (db, mail, service);
    }

    // check if the owner gets one report on Monday morning with last week's numbers and a link to Statistikk
    [Fact]
    public async Task SendDueAsync_MondayMorning_OneReportPerWeek()
    {
        var (db, mail, service) = await CreateAsync();
        await using var _ = db;

        var first = await service.SendDueAsync(MondayMorning);
        var again = await service.SendDueAsync(MondayMorning.AddHours(3));
        var nextWeek = await service.SendDueAsync(MondayMorning.AddDays(7));

        Assert.Equal(1, first);
        Assert.Equal(0, again);
        Assert.Equal(1, nextWeek);
        var report = mail.Sent[0];
        Assert.Equal("eier@test.example", report.To);
        Assert.Equal("Ukerapport for Klipp & Stil Frisør: 1 besøk forrige uke", report.Subject);
        Assert.Contains("Slik gikk uke 1 for Klipp & Stil Frisør (31. desember – 6. januar)", report.Body);
        Assert.Contains("Beregnet omsetning: 7 450 kr", report.Body);
        Assert.Contains("https://bestiapp.no/panel/statistikk", report.Body);
    }

    // check if nothing is sent at night, on other days, or when the owner turned it off
    [Fact]
    public async Task SendDueAsync_NotAtNightOtherDaysOrWhenOff()
    {
        var (db, mail, service) = await CreateAsync();
        await using var _ = db;

        Assert.Equal(0, await service.SendDueAsync(MondayMorning.AddHours(-3))); // 05:00
        Assert.Equal(0, await service.SendDueAsync(MondayMorning.AddDays(2))); // Wednesday
        (await db.Businesses.FindAsync(KlippOgStil))!.WeeklyReportOff = true;
        await db.SaveChangesAsync();
        Assert.Equal(0, await service.SendDueAsync(MondayMorning));
        Assert.Empty(mail.Sent);
    }
}
