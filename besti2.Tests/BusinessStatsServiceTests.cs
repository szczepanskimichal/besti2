using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Statistics;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Statistikk" in the panel: visits that took place, estimated revenue, new customers and tips
public class BusinessStatsServiceTests
{
    // Wednesday 9 January 2030, 13:00 in Oslo
    private static readonly DateTime Now = new(2030, 1, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Dameklipp = new("a1111111-0000-0000-0000-000000000001");
    private static readonly Guid Herreklipp = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid Ingrid = new("b1111111-0000-0000-0000-000000000001");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Booking Visit(DateTime startUtc, decimal? price, string? userId, string email = "",
        BookingStatus status = BookingStatus.Bekreftet, Guid? serviceId = null) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = startUtc.AddDays(-3),
        CustomerName = "Kunde",
        CustomerPhone = "+4790000000",
        CustomerEmail = email,
        CustomerUserId = userId,
        Status = status,
        PriceNok = price,
        ServiceId = serviceId ?? Herreklipp,
        EmployeeId = Ingrid
    };

    // check if visits, revenue, new and returning customers and online/manual are counted for the last 30 days
    [Fact]
    public async Task GetAsync_CountsTheLast30DaysAndThe30DaysBefore()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.AddRange(
            Visit(Now.AddDays(-40), 450m, "kari"), // the 30 days before: Kari's first visit
            Visit(Now.AddDays(-10), 450m, "kari"), // Kari comes back
            Visit(Now.AddDays(-5), 790m, null, "ola@test.example", serviceId: Dameklipp), // added by the business
            Visit(Now.AddDays(-1), null, "per"), // no fixed price
            Visit(Now.AddDays(-3), 450m, "lise", status: BookingStatus.MotteIkke),
            Visit(Now.AddDays(-2), 450m, "nils", status: BookingStatus.Avlyst),
            Visit(Now.AddDays(1), 450m, "kari", status: BookingStatus.Venter)); // coming
        await db.SaveChangesAsync();

        var stats = await new BusinessStatsService(db).GetAsync(KlippOgStil, 30, Now);

        Assert.NotNull(stats);
        Assert.Equal(new(3, 1240m, 1, 1, 1, 2, 1, 2, 1), stats.Current);
        Assert.Equal(1, stats.Previous.Visits);
        Assert.Equal(450m, stats.Previous.Revenue);
        Assert.Equal(1, stats.Previous.NewCustomers);
        Assert.Equal(1, stats.UpcomingVisits);
        Assert.Equal(450m, stats.UpcomingRevenue);
        Assert.Equal(12, stats.Weeks.Count);
        Assert.Equal(new DateOnly(2030, 1, 7), stats.Weeks[^1].WeekStart);
        Assert.Equal(3, stats.Weekdays.Sum());
        Assert.Contains(stats.Services, s => s.Name == "Dameklipp" && s.Revenue == 790m);
        Assert.True(stats.WeeklyReportOn);
    }

    // check if only the given choices of days are accepted
    [Fact]
    public async Task GetAsync_OtherNumberOfDays_IsNull()
    {
        await using var db = await CreateSeededDbAsync();

        Assert.Null(await new BusinessStatsService(db).GetAsync(KlippOgStil, 14, Now));
        Assert.Null(await new BusinessStatsService(db).GetAsync(Guid.NewGuid(), 30, Now));
    }

    // check if a new business gets the setup tips first, and never more than two about Pro
    [Fact]
    public async Task GetAsync_Tips_SetupFirstAndAtMostTwoAboutPro()
    {
        await using var db = await CreateSeededDbAsync();
        db.OpeningHours.RemoveRange(db.OpeningHours.Where(o => o.BusinessId == KlippOgStil));
        for (var day = 1; day <= 12; day++)
        {
            db.Bookings.Add(Visit(Now.AddDays(-day), 450m, null, $"kunde{day}@test.example"));
        }
        await db.SaveChangesAsync();

        var stats = await new BusinessStatsService(db).GetAsync(KlippOgStil, 30, Now);

        Assert.Equal("Legg inn åpningstider", stats!.Tips[0].Title);
        Assert.Contains(stats.Tips, t => t.Title == "La kundene bestille selv");
        Assert.InRange(stats.Tips.Count(t => t.Kind == "pro"), 1, 2);
        Assert.Null(stats.OccupancyPercent); // no opening hours
    }

    // check if the price is saved on the booking: fixed price, the hourly price times the hours, none by agreement
    [Fact]
    public void PriceFor_FixedHourlyAndByAgreement()
    {
        var fixedPrice = new Service { PriceType = PriceType.FastPris, PriceNok = 450m };
        var hourly = new Service { PriceType = PriceType.PerTime, PriceNok = 850m };
        var byAgreement = new Service { PriceType = PriceType.EtterAvtale, PriceNok = 500m };

        Assert.Equal(450m, BookingRules.PriceFor(fixedPrice, 1));
        Assert.Equal(2550m, BookingRules.PriceFor(hourly, 3));
        Assert.Null(BookingRules.PriceFor(byAgreement, 1));
    }

    // check if the Ukerapport switch and "Jeg er interessert" are saved
    [Fact]
    public async Task WeeklyReportAndProInterest_AreSaved()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new BusinessStatsService(db);

        Assert.True(await service.SetWeeklyReportAsync(KlippOgStil, false));
        Assert.True(await service.SetProInterestAsync(KlippOgStil, Now));
        Assert.True(await service.SetProInterestAsync(KlippOgStil, Now.AddDays(1)));
        var stats = await service.GetAsync(KlippOgStil, 7, Now);

        Assert.False(stats!.WeeklyReportOn);
        Assert.True(stats.ProInterest);
        Assert.Equal(Now, (await db.Businesses.FindAsync(KlippOgStil))!.ProInterestUtc); // the first press counts
    }
}
