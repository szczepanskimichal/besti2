namespace besti2.Application.Businesses;

public interface IBusinessService
{
    Task<BusinessDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // The public page by its short address (bestiapp.no/klipp-og-stil-frisor-gjovik); null when not found
    Task<BusinessDetailsDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    // The owner's panel: also a business that is still waiting for approval
    Task<BusinessDetailsDto?> GetForPanelAsync(Guid id, CancellationToken cancellationToken = default);

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