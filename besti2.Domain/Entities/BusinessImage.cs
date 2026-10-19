using besti2.Domain.Enums;

namespace besti2.Domain.Entities;

// One row per image; the file itself lives in image storage, not in the database
public class BusinessImage
{
    public Guid Id { get; set; }
    public ImageKind Kind { get; set; }
    public string FileName { get; set; } = null!;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
}