using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// The customer's search: filters, free times on the cards, Nær meg
public class BusinessSearchServiceTests
{
    // A weekday at least a week ahead, so "now" never removes slots in the test
    private static DateOnly Next(DayOfWeek day)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today.AddDays(7 + ((int)day - (int)today.DayOfWeek + 7) % 7);
    }

    private static async Task<(AppDbContext Db, BusinessSearchService Service)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (db, new BusinessSearchService(db, new FakeImageStorage()));
    }

    // check if a business without services (nothing to book yet) is left out of the search and the city list
    [Fact]
    public async Task GetAllAsync_BusinessWithoutServices_IsNotShown()
    {
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;
        foreach (var s in db.Services.Where(s => s.BusinessId == new Guid("33333333-3333-3333-3333-333333333333")))
        {
            s.IsDeleted = true;
        }

        await db.SaveChangesAsync();

        var result = await service.GetAllAsync();
        var cities = await service.GetCitiesAsync();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, b => b.Name == "Nordlys Psykologsenter");
        Assert.DoesNotContain("Trondheim", cities);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSeededBusinesses_SortedByName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessSearchService(db, new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(
            new[] { "Fjord Bygg AS", "Klipp & Stil Frisør", "Nordlys Psykologsenter" },
            result.Select(b => b.Name));
    }

    // check if GetAllAsync filters by category (case-insensitive)
    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessSearchService(db, new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync(category: "frisor");

        // Assert
        var business = Assert.Single(result);
        Assert.Equal("Klipp & Stil Frisør", business.Name);
    }

    // check if GetAllAsync finds a city by part of its name, ignoring case
    [Fact]
    public async Task GetAllAsync_FiltersByPartOfCity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessSearchService(db, new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync(city: "berg");

        // Assert
        var business = Assert.Single(result);
        Assert.Equal("Bergen", business.City);
    }

    // check if GetAllAsync text search matches a service name
    [Fact]
    public async Task GetAllAsync_SearchMatchesServiceName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessSearchService(db, new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync(q: "HERREKLIPP");

        // Assert
        var business = Assert.Single(result);
        Assert.Equal("Klipp & Stil Frisør", business.Name);
    }

    // check if a date shows only open businesses, soonest first, with max 3 free times each
    [Fact]
    public async Task GetAllAsync_WithDate_ReturnsOpenBusinessesSoonestFirst()
    {
        // Arrange
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;

        // Act: Monday - all three seeded businesses are open
        var result = await service.GetAllAsync(date: Next(DayOfWeek.Monday));

        // Assert: Fjord Bygg opens at 07:00, the earliest
        Assert.Equal(3, result.Count);
        Assert.Equal("Fjord Bygg AS", result[0].Name);
        Assert.All(result, b => Assert.Equal(3, b.FreeSlots!.Count));
    }

    // check if closed businesses are left out (Saturday: only Klipp & Stil, Sunday: nobody)
    [Fact]
    public async Task GetAllAsync_WithDate_LeavesOutClosedBusinesses()
    {
        // Arrange
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;

        // Act
        var saturday = await service.GetAllAsync(date: Next(DayOfWeek.Saturday));
        var sunday = await service.GetAllAsync(date: Next(DayOfWeek.Sunday));

        // Assert
        Assert.Equal("Klipp & Stil Frisør", Assert.Single(saturday).Name);
        Assert.Empty(sunday);
    }

    // check if the searched service is the one used for the free times
    [Fact]
    public async Task GetAllAsync_WithDateAndText_UsesTheSearchedService()
    {
        // Arrange
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;

        // Act
        var result = await service.GetAllAsync(q: "herreklipp", date: Next(DayOfWeek.Monday));

        // Assert
        var business = Assert.Single(result);
        Assert.All(business.FreeSlots!, s => Assert.Equal("Herreklipp", s.ServiceName));
    }

    // check if a fully booked business disappears from the results
    [Fact]
    public async Task GetAllAsync_WithDate_LeavesOutFullyBookedBusiness()
    {
        // Arrange
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;
        var monday = Next(DayOfWeek.Monday);
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var dameklippId = new Guid("a1111111-0000-0000-0000-000000000001");
        var klippEmployees = await db.Employees.Where(e => e.BusinessId == klippId).ToListAsync();
        foreach (var employee in klippEmployees)
        {
            // One long booking that covers the whole Oslo day
            db.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                StartUtc = monday.ToDateTime(TimeOnly.MinValue).AddHours(-3),
                EndUtc = monday.ToDateTime(TimeOnly.MinValue).AddHours(27),
                CustomerName = "Opptatt",
                CustomerPhone = "12345678",
                CustomerEmail = "opptatt@example.example",
                Status = BookingStatus.Bekreftet,
                EmployeeId = employee.Id,
                ServiceId = dameklippId,
            });
        }

        await db.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync(date: monday);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, b => b.Id == klippId);
    }

    // check if "før 12" only returns morning times (Oslo time)
    [Fact]
    public async Task GetAllAsync_WithTimeWindow_ReturnsOnlyTimesInsideIt()
    {
        // Arrange
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;
        var oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        // Act
        var result = await service.GetAllAsync(date: Next(DayOfWeek.Monday), to: new TimeOnly(12, 0));

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result.SelectMany(b => b.FreeSlots!), s =>
            Assert.True(TimeZoneInfo.ConvertTimeFromUtc(s.StartUtc, oslo).Hour < 12));
    }

    // check if "etter 17" without a date finds the soonest day that really has evening times
    [Fact]
    public async Task GetAllAsync_TimeOfDayWithoutDate_FindsSoonestDayWithThoseTimes()
    {
        // Arrange: only Klipp & Stil is open after 17 (Thursday until 19)
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;
        var oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

        // Act
        var result = await service.GetAllAsync(from: new TimeOnly(17, 0));

        // Assert
        var business = Assert.Single(result);
        Assert.Equal("Klipp & Stil Frisør", business.Name);
        Assert.All(business.FreeSlots!, s =>
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(s.StartUtc, oslo);
            Assert.True(local.Hour >= 17);
            Assert.Equal(DayOfWeek.Thursday, local.DayOfWeek);
        });
    }

    // check if "Nær meg" puts the nearest business first and fills in the distance
    [Fact]
    public async Task GetAllAsync_WithLocation_SortsByDistance()
    {
        // Arrange: standing at Bryggen in Bergen
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;

        // Act
        var result = await service.GetAllAsync(lat: 60.3975, lng: 5.3240);

        // Assert
        Assert.Equal(
            new[] { "Fjord Bygg AS", "Klipp & Stil Frisør", "Nordlys Psykologsenter" },
            result.Select(b => b.Name));
        Assert.True(result[0].DistanceKm < 1);
    }

    // check if "Nær meg" with a radius leaves out businesses further away
    [Fact]
    public async Task GetAllAsync_WithRadius_OnlyNearBusinesses()
    {
        // Arrange: standing at Bryggen in Bergen
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;

        // Act: max 50 km (Oslo and Trondheim are much further)
        var result = await service.GetAllAsync(lat: 60.3975, lng: 5.3240, maxKm: 50);

        // Assert
        Assert.Equal(new[] { "Fjord Bygg AS" }, result.Select(b => b.Name));
    }

    // check if "Meg + en venn" shows only businesses with two employees free at the same time, with those times
    [Fact]
    public async Task GetAllAsync_TwoPersons_OnlyBusinessesThatTakeTwo()
    {
        var (db, service) = await CreateServiceAsync();
        await using var _ = db;

        var alone = await service.GetAllAsync(date: Next(DayOfWeek.Monday));
        var together = await service.GetAllAsync(date: Next(DayOfWeek.Monday), persons: 2);
        var soonest = await service.GetAllAsync(persons: 2);

        Assert.Contains(alone, b => b.Name == "Fjord Bygg AS");
        var klipp = Assert.Single(together);
        Assert.Equal("Klipp & Stil Frisør", klipp.Name);
        Assert.NotEmpty(klipp.FreeSlots!);
        Assert.DoesNotContain(soonest, b => b.Name == "Fjord Bygg AS");
        Assert.Contains(soonest, b => b.Name == "Klipp & Stil Frisør" && b.FreeSlots!.Count > 0);
    }
}
