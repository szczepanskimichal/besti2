using besti2.Application.Images;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BusinessImageService(AppDbContext dbContext, IImageStorage storage) : IBusinessImageService
{
    private const long MaxLogoBytes = 500 * 1024;

    public async Task<string?> SetLogoAsync(Guid businessId, Stream content, long length,
        CancellationToken cancellationToken = default)
    {
        // Check the size before reading, so a huge upload is never loaded into memory
        if (length == 0 || length > MaxLogoBytes)
        {
            return "Logoen må være mindre enn 500 KB.";
        }

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var data = memory.ToArray();

        var extension = DetectImageType(data);
        if (extension is null)
        {
            return "Bare PNG, JPG eller WebP er tillatt.";
        }

        if (!await dbContext.Businesses.AnyAsync(b => b.Id == businessId, cancellationToken))
        {
            return "Fant ikke bedriften.";
        }

        // Our own random name; the uploaded file name is never used
        var fileName = $"{Guid.NewGuid()}{extension}";
        await storage.SaveAsync(fileName, new MemoryStream(data), cancellationToken);

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

    private static bool StartsWith(byte[] data, byte[] prefix) =>
        data.Length >= prefix.Length && data.Take(prefix.Length).SequenceEqual(prefix);
}