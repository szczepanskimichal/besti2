using besti2.Application.Images;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace besti2.Infrastructure.Services;

public class BusinessImageService(AppDbContext dbContext, IImageStorage storage) : IBusinessImageService
{
    private const long MaxUploadBytes = 10 * 1024 * 1024; // enough for any phone photo
    
    private const int LogoMaxSize = 512; // longest side in pixels after resizing
    public async Task<string?> SetLogoAsync(Guid businessId, Stream content, long length,
        CancellationToken cancellationToken = default)
    {
        // Check the size before reading, so a huge upload is never loaded into memory
        if (length == 0)
        {
            return "Velg et bilde.";
        }
        
        if (length > MaxUploadBytes)
        {
            return "Bildet kan være maks 10 MB.";
        }

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var data = memory.ToArray();

        if (DetectImageType(data) is null)
        {
            return "Bare PNG, JPG eller WebP er tillatt.";
        }

        if (!await dbContext.Businesses.AnyAsync(b => b.Id == businessId, cancellationToken))
        {
            return "Fant ikke bedriften.";
        }
        var resized = ResizeToWebp(data, LogoMaxSize);
        
        if (resized is null)
        {
            return "Kunne ikke lese bildet.";
        }

        // Our own random name; the uploaded file name is never used
        var fileName = $"{Guid.NewGuid()}.webp";
        await storage.SaveAsync(fileName, new MemoryStream(resized), cancellationToken);

        var oldLogo = await dbContext.BusinessImages
            .FirstOrDefaultAsync(i => i.BusinessId == businessId && i.Kind == ImageKind.Logo, cancellationToken);
        if (oldLogo is not null)
        {
            dbContext.BusinessImages.Remove(oldLogo);
        }

        dbContext.BusinessImages.Add(new BusinessImage
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Kind = ImageKind.Logo,
            FileName = fileName,
            CreatedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        // Delete the old file only after the database points to the new one
        if (oldLogo is not null)
        {
            await storage.DeleteAsync(oldLogo.FileName, cancellationToken);
        }

        return null;
    }

    // Real file type from the first bytes ("magic numbers"), not from the file name
    private static string? DetectImageType(byte[] data)
    {
        if (StartsWith(data, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return ".png";
        }

        if (StartsWith(data, [0xFF, 0xD8, 0xFF]))
        {
            return ".jpg";
        }

        if (StartsWith(data, "RIFF"u8.ToArray()) && data.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray()))
        {
            return ".webp";
        }

        return null;
    }
    // Fits the image inside maxSize x maxSize(keeps proportions) and saves it as WebP.

// A new file is created, so EXIF data like GPS location is not kept.
    private static byte[]? ResizeToWebp(byte[] data, int maxSize)
    {
        using var codec = SKCodec.Create(new MemoryStream(data));
        if (codec is null)
        {
            return null;
        }

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null)
        {
            return null;
        }

// Phone photos are often stored sideways with a "rotate me" flag; apply it
        using var upright = ApplyOrientation(decoded, codec.EncodedOrigin);
        var scale = Math.Min(1.0, (double)maxSize / Math.Max(upright.Width, upright.Height));
        var width = Math.Max(1, (int)Math.Round(upright.Width * scale));
        var height = Math.Max(1, (int)Math.Round(upright.Height * scale));
        using var resized = upright.Resize(new SKImageInfo(width, height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        if (resized is null)
        {
            return null;
        }
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 80);
        return encoded?.ToArray();
    }

// Returns an upright copy of the bitmap (handles 90, 180 and 270 degree photos)
    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        var degrees = origin switch
        {
            SKEncodedOrigin.BottomRight => 180,
            SKEncodedOrigin.RightTop => 90,
            SKEncodedOrigin.LeftBottom => 270,
            _ => 0
        };

        var swap = degrees is 90 or 270;
        var result = new SKBitmap(swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height);
        using var canvas = new SKCanvas(result);
        canvas.Translate(result.Width / 2f, result.Height / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawBitmap(bitmap, 0, 0);
        return result;
    }

    private static bool StartsWith(byte[] data, byte[] prefix) =>
        data.Length >= prefix.Length && data.Take(prefix.Length).SequenceEqual(prefix);
}