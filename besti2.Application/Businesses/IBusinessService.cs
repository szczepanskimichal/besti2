namespace besti2.Application.Businesses;

public interface IBusinessService
{
    Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(
        string? category = null, string? city = null,
        CancellationToken cancellationToken = default);
    Task<BusinessDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default);
    // Returns null when saved, otherwise an error message for the owner
    Task<string?> AddServiceAsync(Guid businessId, CreateServiceRequest request,
        CancellationToken cancellationToken = default);
    // Returns null when saved, otherwise an error message for the owner
    Task<string?> UpdateServiceAsync(Guid businessId, Guid serviceId, CreateServiceRequest request,
        CancellationToken cancellationToken = default);

    // Soft delete: returns false when the service was not found
    Task<bool> DeleteServiceAsync(Guid businessId, Guid serviceId,
        CancellationToken cancellationToken = default);
    
    // Owner edits description and contact info; returns null when saved, otherwise an error message
    Task<string?> UpdateProfileAsync(Guid businessId, UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
    
    // Replaces the weekly opening hours; days missing from the list are closed
    Task<string?> UpdateOpeningHoursAsync(Guid businessId, UpdateOpeningHoursRequest request,
        CancellationToken cancellationToken = default);
}