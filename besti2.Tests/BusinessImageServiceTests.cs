using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

public class BusinessImageServiceTests
{
    private static readonly Guid KlippId = new("11111111-1111-1111-1111-111111111111");

    // Smallest "PNG": the 8-byte signature plus some bytes
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private static async Task<(BusinessImageService Service, AppDbContext Db, FakeImageStorage Storage)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var storage = new FakeImageStorage();
        return (new BusinessImageService(db, storage), db, storage);
    }

    [Fact]
    public async Task SetLogoAsync_SavesPngLogo()
    {
        var (service, db, storage) = await CreateAsync();

        var error = await service.SetLogoAsync(KlippId, new MemoryStream(Png), Png.Length);

        Assert.Null(error);
        var logo = Assert.Single(db.BusinessImages.Where(i => i.BusinessId == KlippId && i.Kind == ImageKind.Logo));
        Assert.EndsWith(".png", logo.FileName);
        Assert.True(storage.Files.ContainsKey(logo.FileName));
    }

    [Fact]
    public async Task SetLogoAsync_RejectsFileThatIsNotAnImage()
    {
        var (service, db, storage) = await CreateAsync();
        // Text file renamed to .png must still be rejected
        var text = "this is not an image"u8.ToArray();

        var error = await service.SetLogoAsync(KlippId, new MemoryStream(text), text.Length);

        Assert.Equal("Bare PNG, JPG eller WebP er tillatt.", error);
        Assert.Empty(db.BusinessImages);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task SetLogoAsync_ReplacesOldLogo()
    {
        var (service, db, storage) = await CreateAsync();

        await service.SetLogoAsync(KlippId, new MemoryStream(Png), Png.Length);
        await service.SetLogoAsync(KlippId, new MemoryStream(Png), Png.Length);

        var logo = Assert.Single(db.BusinessImages.Where(i => i.BusinessId == KlippId));
        Assert.Single(storage.Files);
        Assert.True(storage.Files.ContainsKey(logo.FileName));
    }
}