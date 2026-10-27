using besti2.Application.Images;
using Microsoft.Extensions.Configuration;

namespace besti2.Infrastructure.Services;

// Development storage: files in the API's "uploads" folder, served at /uploads
public class LocalImageStorage(IConfiguration configuration) : IImageStorage
{
    private readonly string _folder = Path.Combine(
        configuration["contentRoot"] ?? Directory.GetCurrentDirectory(), "uploads");

    private readonly string _publicBaseUrl =
        (configuration["ImageStorage:PublicBaseUrl"] ?? "http://localhost:5156/uploads").TrimEnd('/');

    public async Task SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_folder);
        await using var file = File.Create(GetPath(fileName));
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public string GetUrl(string fileName) => $"{_publicBaseUrl}/{fileName}";

    // Only a plain file name is allowed, so "../" can never leave the folder
    private string GetPath(string fileName) =>
        Path.GetFileName(fileName) == fileName && fileName.Length > 0
            ? Path.Combine(_folder, fileName)
            : throw new ArgumentException("Invalid file name.", nameof(fileName));
}