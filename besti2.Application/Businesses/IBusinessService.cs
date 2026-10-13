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
}