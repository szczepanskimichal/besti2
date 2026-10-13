namespace besti2.Application.Businesses;

public interface IBusinessRegistrationService
{
    Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request, CancellationToken cancellationToken = default);
}