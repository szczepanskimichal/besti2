namespace besti2.Application.Images;

public interface IBusinessImageService
{
    // Returns null when saved, otherwise an error message
    Task<string?> SetLogoAsync(Guid businessId, Stream content, long length,
        CancellationToken cancellationToken = default);

    // Gallery: returns null when saved, otherwise an error message
    Task<string?> AddGalleryImageAsync(Guid businessId, Stream content, long length,
        CancellationToken cancellationToken = default);

    // A customer's photo on a review: checked and saved as WebP like the gallery; returns the file name or the error
    Task<(string? FileName, string? Error)> SaveReviewPhotoAsync(Stream content, long length,
        CancellationToken cancellationToken = default);

    // false = not found (or another business's image)
    Task<bool> DeleteGalleryImageAsync(Guid businessId, Guid imageId,
        CancellationToken cancellationToken = default);
}