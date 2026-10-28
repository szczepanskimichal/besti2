using besti2.Application.Businesses;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Forslag for din bransje" on /panel/tjenester
public class ServiceTemplateTests
{
    private static readonly Guid KlippId = new("11111111-1111-1111-1111-111111111111");

    private static async Task<(BusinessService Service, AppDbContext Db)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (new BusinessService(db, new FakeGeocodingService(), new FakeImageStorage()), db);
    }

    // check if every template could be saved as a normal service (the same rules as the form)
    [Fact]
    public void Templates_AreValidServices()
    {
        foreach (var category in Enum.GetValues<BusinessCategory>())
        {
            var templates = ServiceTemplates.For(category);
            Assert.True(templates.Count <= ServiceTemplates.MaxPerRequest);
            Assert.Equal(templates.Count, templates.Select(t => t.Name.ToLowerInvariant()).Distinct().Count());
            foreach (var t in templates)
            {
                Assert.InRange(t.DurationMinutes, 5, 480);
                Assert.Equal(0, t.DurationMinutes % 5);
                Assert.True(Enum.IsDefined(Enum.Parse<PriceType>(t.PriceType)));
                Assert.Equal(t.PriceType == nameof(PriceType.EtterAvtale), t.PriceNok is null);
            }
        }

        // Every industry except "Annet" has suggestions
        Assert.All(Enum.GetValues<BusinessCategory>().Where(c => c != BusinessCategory.Annet),
            c => Assert.NotEmpty(ServiceTemplates.For(c)));
    }

    // check if the suggestions follow the category and leave out services the business already has
    [Fact]
    public async Task GetServiceTemplatesAsync_SkipsExistingNames()
    {
        var (service, _) = await CreateServiceAsync();

        var templates = await service.GetServiceTemplatesAsync(KlippId);

        // Klipp & Stil already has Dameklipp and Herreklipp
        Assert.DoesNotContain(templates, t => t.Name is "Dameklipp" or "Herreklipp");
        Assert.Contains(templates, t => t.Name == "Barneklipp (under 12 år)");
        Assert.Empty(await service.GetServiceTemplatesAsync(Guid.NewGuid()));
    }

    // check if the chosen suggestions are saved together, with the owner's own price
    [Fact]
    public async Task AddServicesAsync_SavesAll()
    {
        var (service, db) = await CreateServiceAsync();

        var error = await service.AddServicesAsync(KlippId,
        [
            new CreateServiceRequest("Barneklipp", null, 30, 299m, "FastPris"),
            new CreateServiceRequest(" Vask og føn ", "Rask", 30, 399m, "FastPris")
        ]);

        Assert.Null(error);
        Assert.Equal(5, await db.Services.CountAsync(s => s.BusinessId == KlippId && !s.IsDeleted));
        Assert.Equal(299m, (await db.Services.SingleAsync(s => s.Name == "Barneklipp")).PriceNok);
        Assert.True(await db.Services.AnyAsync(s => s.Name == "Vask og føn"));
    }

    // check if one wrong service stops all of them (no half-saved list)
    [Fact]
    public async Task AddServicesAsync_OneInvalid_SavesNothing()
    {
        var (service, db) = await CreateServiceAsync();
        var before = await db.Services.CountAsync();

        var error = await service.AddServicesAsync(KlippId,
        [
            new CreateServiceRequest("Barneklipp", null, 30, 299m, "FastPris"),
            new CreateServiceRequest("Farge", null, 30, -5m, "FastPris")
        ]);

        Assert.Equal("Farge: Prisen kan ikke være negativ.", error);
        Assert.Equal(before, await db.Services.CountAsync());
    }

    // check if an empty or too long list is refused
    [Fact]
    public async Task AddServicesAsync_EmptyOrTooMany_Fails()
    {
        var (service, _) = await CreateServiceAsync();
        var many = Enumerable.Range(1, ServiceTemplates.MaxPerRequest + 1)
            .Select(i => new CreateServiceRequest($"Tjeneste {i}", null, 30, 100m, "FastPris"))
            .ToList();

        Assert.Equal("Velg minst én tjeneste.", await service.AddServicesAsync(KlippId, []));
        Assert.NotNull(await service.AddServicesAsync(KlippId, many));
    }
}
