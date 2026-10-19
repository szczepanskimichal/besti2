namespace besti2.Application.Businesses;

public interface IBusinessService
{
    Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(
        string? category = null, string? city = null, string? q = null,
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
    Task<string?> AddEmployeeAsync(Guid businessId, EmployeeRequest request,
        CancellationToken cancellationToken = default);
    Task<string?> UpdateEmployeeAsync(Guid businessId, Guid employeeId, EmployeeRequest request,
        CancellationToken cancellationToken = default);
    // Soft delete; returns an error when not found or when it is the last employee
    Task<string?> DeleteEmployeeAsync(Guid businessId, Guid employeeId,
        CancellationToken cancellationToken = default);
    
    // Owner edits description and contact info; returns null when saved, otherwise an error message
    Task<string?> UpdateProfileAsync(Guid businessId, UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
    
    // Replaces the weekly opening hours; days missing from the list are closed
    Task<string?> UpdateOpeningHoursAsync(Guid businessId, UpdateOpeningHoursRequest request,
        CancellationToken cancellationToken = default);
}