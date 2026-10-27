namespace besti2.Application.Businesses;

public record BusinessDetailsDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string? Email,
    string PhoneNumber,
    string Address,
    string PostalCode,
    string City,
    IReadOnlyList<ServiceDto> Services,
    IReadOnlyList<EmployeeDto> Employees,
    IReadOnlyList<OpeningHoursDto> OpeningHours,
    double? Latitude,
    double? Longitude,
    string? LogoUrl,
    IReadOnlyList<GalleryImageDto> Gallery,
    // Only true in the owner's panel: the business is waiting for approval and customers cannot see it
    bool IsHidden,
    // Free cancellation until X hours before the visit; 0 = any time
    int FreeCancelHours
);