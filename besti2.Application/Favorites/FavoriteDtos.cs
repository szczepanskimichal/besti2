namespace besti2.Application.Favorites;

// "Favoritter" in Min Besti: the saved business, the customer's last visit there (for "Bestill igjen" with
// the same service and employee) and the next booked visit, if any
public record FavoriteDto(
    Guid BusinessId,
    string Name,
    string Slug,
    string City,
    string Category,
    string? LogoUrl,
    double? AverageRating,
    int ReviewCount,
    FavoriteLastVisitDto? LastVisit,
    DateTime? NextVisitUtc);

public record FavoriteLastVisitDto(DateTime StartUtc, Guid ServiceId, string ServiceName, Guid EmployeeId,
    string EmployeeName, bool ServiceAvailable, bool EmployeeAvailable);
