namespace besti2.Application.Images;

// Stores image files; the database keeps only the file name
public interface IImageStorage
{
    Task SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
    string GetUrl(string fileName);
}