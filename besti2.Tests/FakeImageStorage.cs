using besti2.Application.Images;

namespace besti2.Tests;

public class FakeImageStorage : IImageStorage
{
    public Dictionary<string, byte[]> Files { get; } = new();

    public async Task SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        Files[fileName] = memory.ToArray();
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        Files.Remove(fileName);
        return Task.CompletedTask;
    }

    public string GetUrl(string fileName) => $"https://images.example/{fileName}";
}