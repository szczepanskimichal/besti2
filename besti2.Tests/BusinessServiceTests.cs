using besti2.Application.Businesses;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

public class BusinessServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsSeededBusinesses_SortedByName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(
            new[] { "Fjord Bygg AS", "Klipp & Stil Frisør", "Nordlys Psykologsenter" },
            result.Select(b => b.Name));
    }

    // check if GetByIdAsync returns the correct business with its services and employees -happyPath
    [Fact]
    public async Task GetByIdAsync_ReturnsBusinessWithServicesAndEmployees()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");

        // Act
        var result = await service.GetByIdAsync(klippId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Klipp & Stil Frisør", result.Name);
        Assert.Equal(
            new[] { "Dameklipp", "Farging", "Herreklipp" },
            result.Services.Select(s => s.Name));
        Assert.Equal(2, result.Employees.Count);
    }

    // check if GetByIdAsync returns null when the business does not exist - unhappy path
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenBusinessDoesNotExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
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
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync(category: "frisor");

        // Assert
        var business = Assert.Single(result);
        Assert.Equal("Klipp & Stil Frisør", business.Name);
    }

    // check if AddServiceAsync saves a new service for the business - happy path
    [Fact]
    public async Task AddServiceAsync_SavesService()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new CreateServiceRequest("Skjeggtrim", null, 20, 199, "FastPris");

        // Act
        var error = await service.AddServiceAsync(klippId, request);

        // Assert
        Assert.Null(error);
        var business = await service.GetByIdAsync(klippId);
        Assert.Contains(business!.Services, s => s.Name == "Skjeggtrim" && s.DurationMinutes == 20);
    }

    // check if AddServiceAsync rejects an unknown price type - unhappy path
    [Fact]
    public async Task AddServiceAsync_ReturnsError_WhenPriceTypeIsInvalid()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new CreateServiceRequest("Skjeggtrim", null, 20, 199, "Gratis");

        // Act
        var error = await service.AddServiceAsync(klippId, request);

        // Assert
        Assert.Equal("Velg en gyldig pristype.", error);
    }

    // check if UpdateServiceAsync changes the service - happy path
    [Fact]
    public async Task UpdateServiceAsync_ChangesService()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var dameklippId = new Guid("a1111111-0000-0000-0000-000000000001");
        var request = new CreateServiceRequest("Dameklipp lang", null, 90, 990, "FastPris");

        // Act
        var error = await service.UpdateServiceAsync(klippId, dameklippId, request);

        // Assert
        Assert.Null(error);
        var business = await service.GetByIdAsync(klippId);
        Assert.Contains(business!.Services, s => s.Name == "Dameklipp lang" && s.DurationMinutes == 90);
    }

    // check if an owner cannot edit another business's service - security
    [Fact]
    public async Task UpdateServiceAsync_ReturnsError_ForOtherBusinessService()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var fjordByggId = new Guid("22222222-2222-2222-2222-222222222222");
        var dameklippId = new Guid("a1111111-0000-0000-0000-000000000001");
        var request = new CreateServiceRequest("Hacket", null, 30, 1, "FastPris");

        // Act
        var error = await service.UpdateServiceAsync(fjordByggId, dameklippId, request);

        // Assert
        Assert.Equal("Fant ikke tjenesten.", error);
    }

    // check if DeleteServiceAsync hides the service and cannot delete it twice
    [Fact]
    public async Task DeleteServiceAsync_HidesService()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var dameklippId = new Guid("a1111111-0000-0000-0000-000000000001");

        // Act
        var deleted = await service.DeleteServiceAsync(klippId, dameklippId);
        var deletedAgain = await service.DeleteServiceAsync(klippId, dameklippId);

        // Assert
        Assert.True(deleted);
        Assert.False(deletedAgain);
        var business = await service.GetByIdAsync(klippId);
        Assert.DoesNotContain(business!.Services, s => s.Id == dameklippId);
    }

    // check if UpdateOpeningHoursAsync changes hours and closes days missing from the list
    [Fact]
    public async Task UpdateOpeningHoursAsync_ReplacesWeek()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new UpdateOpeningHoursRequest([
            new OpeningHoursDto(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(14, 0)),
            new OpeningHoursDto(DayOfWeek.Saturday, new TimeOnly(10, 0), new TimeOnly(15, 0))
        ]);

        // Act
        var error = await service.UpdateOpeningHoursAsync(klippId, request);

        // Assert
        Assert.Null(error);
        var business = await service.GetByIdAsync(klippId);
        Assert.Equal(2, business!.OpeningHours.Count);
        Assert.Equal(new TimeOnly(14, 0), business.OpeningHours.Single(h => h.DayOfWeek == DayOfWeek.Monday).ClosesAt);
    }

    // check if UpdateOpeningHoursAsync rejects closing time before opening time
    [Fact]
    public async Task UpdateOpeningHoursAsync_ReturnsError_WhenClosesBeforeOpens()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new UpdateOpeningHoursRequest([
            new OpeningHoursDto(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(9, 0))
        ]);

        // Act
        var error = await service.UpdateOpeningHoursAsync(klippId, request);

        // Assert
        Assert.Equal("Stengetid må være etter åpningstid.", error);
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
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

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
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        // Act
        var result = await service.GetAllAsync(q: "HERREKLIPP");

        // Assert
        var business = Assert.Single(result);
        Assert.Equal("Klipp & Stil Frisør", business.Name);
    }

    // check if a new address gets new map coordinates
    [Fact]
    public async Task UpdateProfileAsync_UpdatesLocation_WhenAddressChanges()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var geocoding = new FakeGeocodingService { Result = (60.39, 5.32) };
        var service = new BusinessService(db, geocoding, new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new UpdateProfileRequest(null, "+4799999999", "Nygata 5", "5003", "Bergen");

        // Act
        await service.UpdateProfileAsync(klippId, request);

        // Assert
        var business = await service.GetByIdAsync(klippId);
        Assert.Equal(60.39, business!.Latitude);
        Assert.Equal(5.32, business.Longitude);
    }

    // check if UpdateProfileAsync saves description and contact info - happy path
    [Fact]
    public async Task UpdateProfileAsync_ChangesProfile()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new UpdateProfileRequest("Ny beskrivelse", "+4799999999", "Nygata 5", "5003", "Bergen");

        // Act
        var error = await service.UpdateProfileAsync(klippId, request);

        // Assert
        Assert.Null(error);
        var business = await service.GetByIdAsync(klippId);
        Assert.Equal("Ny beskrivelse", business!.Description);
        Assert.Equal("Bergen", business.City);
    }

    // check if UpdateProfileAsync rejects an invalid postal code
    [Fact]
    public async Task UpdateProfileAsync_ReturnsError_WhenPostalCodeIsInvalid()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new UpdateProfileRequest(null, "+4799999999", "Nygata 5", "12345", "Bergen");

        // Act
        var error = await service.UpdateProfileAsync(klippId, request);

        // Assert
        Assert.Equal("Postnummer må være 4 siffer.", error);
    }

    [Fact]
    public async Task AddEmployeeAsync_AddsEmployee()

    {
// Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        var klippId = new Guid("11111111-1111-1111-1111-111111111111");

// Act

        var error = await service.AddEmployeeAsync(klippId, new EmployeeRequest("  Nora ", "Lie"));

// Assert

        Assert.Null(error);

        var business = await service.GetByIdAsync(klippId);

        Assert.Contains(business!.Employees, e => e.Name == "Nora" && e.LastName == "Lie");
    }

// check if DeleteEmployeeAsync hides the employee

    [Fact]
    public async Task DeleteEmployeeAsync_HidesEmployee()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var sofieId = new Guid("b1111111-0000-0000-0000-000000000002");

// Act
        var error = await service.DeleteEmployeeAsync(klippId, sofieId);

// Assert
        Assert.Null(error);
        var business = await service.GetByIdAsync(klippId);
        Assert.DoesNotContain(business!.Employees, e => e.Id == sofieId);
    }

// check if the last employee cannot be deleted - unhappy path

    [Fact]
    public async Task DeleteEmployeeAsync_ReturnsError_ForLastEmployee()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var fjordByggId = new Guid("22222222-2222-2222-2222-222222222222");
        var larsId = new Guid("b2222222-0000-0000-0000-000000000001");

// Act
        var error = await service.DeleteEmployeeAsync(fjordByggId, larsId);

// Assert
        Assert.Equal("Bedriften må ha minst én ansatt.", error);
    }

// check if a business cannot edit another business's employee - unhappy path

    [Fact]
    public async Task UpdateEmployeeAsync_ReturnsError_ForOtherBusinessEmployee()
    {
// Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var larsId = new Guid("b2222222-0000-0000-0000-000000000001");

        // Act
        var error = await service.UpdateEmployeeAsync(klippId, larsId, new EmployeeRequest("Hacker", ""));

        // Assert
        Assert.Equal("Fant ikke den ansatte.", error);
    }
    // A weekday at least a week ahead, so "now" never removes slots in the test

    private static DateOnly Next(DayOfWeek day)

    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return today.AddDays(7 + ((int)day - (int)today.DayOfWeek + 7) % 7);
    }

    private static async Task<(AppDbContext Db, BusinessService Service)> CreateServiceAsync()

    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        return (db, new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage()));
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
    
    // check if a hidden business (owner has not confirmed the email) is not listed and has no page
    [Fact]
    public async Task HiddenBusiness_IsNotListedAndHasNoPage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        (await db.Businesses.FindAsync(klippId))!.IsHidden = true;
        await db.SaveChangesAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());

        var list = await service.GetAllAsync();
        var page = await service.GetByIdAsync(klippId);

        Assert.DoesNotContain(list, b => b.Id == klippId);
        Assert.Null(page);
        // The owner's panel still opens while the business waits for approval
        var panel = await service.GetForPanelAsync(klippId);
        Assert.NotNull(panel);
        Assert.True(panel.IsHidden);
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
    // check if the owner can set a public contact e-mail, but not the private login e-mail
    [Fact]
    public async Task UpdateProfileAsync_ContactEmail_CannotBeTheLoginEmail()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        db.Users.Add(new AppUser
        {
            UserName = "eier@klippogstil.example", Email = "eier@klippogstil.example",
            NormalizedEmail = "EIER@KLIPPOGSTIL.EXAMPLE", BusinessId = klippId
        });
        await db.SaveChangesAsync();
        var service = new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage());
        var request = new UpdateProfileRequest(null, "+4799999999", "Nygata 5", "5003", "Bergen");

        // Act
        var loginError = await service.UpdateProfileAsync(klippId, request with { ContactEmail = "eier@klippogstil.example" });
        var okError = await service.UpdateProfileAsync(klippId, request with { ContactEmail = "Hei@KlippOgStil.example" });

        // Assert
        Assert.StartsWith("Bruk en annen e-post til kunder", loginError);
        Assert.Null(okError);
        var business = await service.GetByIdAsync(klippId);
        Assert.Equal("hei@klippogstil.example", business!.Email);
    }
}