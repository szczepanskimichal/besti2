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
        var service = new BusinessService(db);

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
        var service = new BusinessService(db);
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
        var service = new BusinessService(db);

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
        var service = new BusinessService(db);

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
        var service = new BusinessService(db);
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
        var service = new BusinessService(db);
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        var request = new CreateServiceRequest("Skjeggtrim", null, 20, 199, "Gratis");

        // Act
        var error = await service.AddServiceAsync(klippId, request);

        // Assert
        Assert.Equal("Velg en gyldig pristype.", error);
    }
}