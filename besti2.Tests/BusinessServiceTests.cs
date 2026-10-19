using besti2.Application.Businesses;
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
}