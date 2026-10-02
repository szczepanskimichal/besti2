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
}