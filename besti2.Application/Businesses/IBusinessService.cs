using besti2.Application.Bookings;
using besti2.Domain.Enums;

namespace besti2.Application.Businesses;

public interface IBusinessService
{
    Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BusinessDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
   
}