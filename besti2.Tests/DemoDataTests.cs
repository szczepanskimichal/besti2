using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

public class DemoDataTests
{
    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    // check if the demo businesses with their services, employees and opening hours are removed, and a real one stays
    [Fact]
    public async Task RemoveAsync_RemovesDemoBusinessesOnly()
    {
        await using var db = await CreateSeededDbAsync();
        var real = new Business
        {
            Id = Guid.NewGuid(), Name = "Ekte Frisør", OrgNumber = "912345688", Category = BusinessCategory.Frisor,
            Email = "post@ekte.example", PhoneNumber = "+4791234567", Address = "Gata 1", PostalCode = "0150", City = "Oslo"
        };
        db.Businesses.Add(real);
        db.Employees.Add(new Employee { Id = Guid.NewGuid(), Name = "Ola", LastName = "Hansen", BusinessId = real.Id });
        await db.SaveChangesAsync();

        var removed = await DemoData.RemoveAsync(db);

        Assert.Equal(3, removed);
        Assert.Equal(real.Id, Assert.Single(await db.Businesses.ToListAsync()).Id);
        Assert.Single(await db.Employees.ToListAsync());
        Assert.Empty(await db.Services.ToListAsync());
        Assert.Empty(await db.OpeningHours.ToListAsync());
    }

    // check if a second start does nothing
    [Fact]
    public async Task RemoveAsync_SecondTime_DoesNothing()
    {
        await using var db = await CreateSeededDbAsync();
        await DemoData.RemoveAsync(db);

        Assert.Equal(0, await DemoData.RemoveAsync(db));
    }
}