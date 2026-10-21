namespace besti2.Application.Businesses;

public interface IBusinessRegistrationService
{
    Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request, CancellationToken cancellationToken = default);
    Task ShowBusinessOfOwnerAsync(string userId, CancellationToken cancellationToken = default);
}