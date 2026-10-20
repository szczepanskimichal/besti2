using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace besti2.Tests;

public class BusinessImageServiceTests
{
    private static readonly Guid KlippId = new("11111111-1111-1111-1111-111111111111");

     
// A real PNG in memory, so the service can decode and resize it
    
    private static byte[] RealPng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

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

        var png = RealPng(100, 100);
        
        var error = await service.SetLogoAsync(KlippId, new MemoryStream(png), png.Length);
        
        Assert.Null(error);
        var logo = Assert.Single(db.BusinessImages.Where(i => i.BusinessId == KlippId && i.Kind == ImageKind.Logo));
        Assert.EndsWith(".webp", logo.FileName);
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

        var png = RealPng(100, 100);
        
        await service.SetLogoAsync(KlippId, new MemoryStream(png), png.Length);
        await service.SetLogoAsync(KlippId, new MemoryStream(png), png.Length);

        var logo = Assert.Single(db.BusinessImages.Where(i => i.BusinessId == KlippId));
        Assert.Single(storage.Files);
        Assert.True(storage.Files.ContainsKey(logo.FileName));
    }
    
    [Fact]
    
    public async Task SetLogoAsync_ResizesLargeImage()
    {
        var (service, db, storage) = await CreateAsync();
        var png = RealPng(2000, 1000);
        await service.SetLogoAsync(KlippId, new MemoryStream(png), png.Length);
        var saved = storage.Files[db.BusinessImages.Single().FileName];
        using var bitmap = SKBitmap.Decode(saved);
        Assert.Equal(512, bitmap.Width);
        Assert.Equal(256, bitmap.Height);
    }
     
    // check if a gallery photo is saved as WebP
    [Fact]
    public async Task AddGalleryImageAsync_SavesImage()
    {
        var (service, db, storage) = await CreateAsync();
        var png = RealPng(50, 50);
        var error = await service.AddGalleryImageAsync(KlippId, new MemoryStream(png), png.Length);
        Assert.Null(error);
        var image = Assert.Single(db.BusinessImages.Where(i => i.Kind == ImageKind.Galleri));
        Assert.True(storage.Files.ContainsKey(image.FileName));
    }
    
    // check if the 13th photo is refused

    [Fact]
    public async Task AddGalleryImageAsync_StopsAtTwelveImages()
    {
        var (service, _, _) = await CreateAsync();
        var png = RealPng(20, 20);
        for (var i = 0; i < 12; i++)
        {
            await service.AddGalleryImageAsync(KlippId, new MemoryStream(png), png.Length);
        }
        var error = await service.AddGalleryImageAsync(KlippId, new MemoryStream(png), png.Length);
        Assert.StartsWith("Galleriet kan ha maks 12 bilder", error);
    }
    
    // check if a business cannot delete another business's photo

    [Fact]
    public async Task DeleteGalleryImageAsync_OnlyDeletesOwnImage()
    {
        var (service, db, storage) = await CreateAsync();
        var png = RealPng(20, 20);
        await service.AddGalleryImageAsync(KlippId, new MemoryStream(png), png.Length);
        var image = db.BusinessImages.Single(i => i.Kind == ImageKind.Galleri);
        var otherBusinessId = new Guid("22222222-2222-2222-2222-222222222222");
        var deletedByOther = await service.DeleteGalleryImageAsync(otherBusinessId, image.Id);
        var deletedByOwner = await service.DeleteGalleryImageAsync(KlippId, image.Id);
        Assert.False(deletedByOther);
        Assert.True(deletedByOwner);
        Assert.Empty(db.BusinessImages.Where(i => i.Kind == ImageKind.Galleri));
        Assert.False(storage.Files.ContainsKey(image.FileName));
    }
}