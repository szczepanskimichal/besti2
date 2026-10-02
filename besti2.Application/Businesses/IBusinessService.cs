namespace besti2.Application.Businesses;

public interface IBusinessService
{
    Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
}