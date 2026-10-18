namespace besti2.Application.Images;

public interface IBusinessImageService
{
    // Returns null when saved, otherwise an error message
    Task<string?> SetLogoAsync(Guid businessId, Stream content, long length,
        CancellationToken cancellationToken = default);
}