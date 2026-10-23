namespace besti2.Application.Businesses;

public interface IBusinessRegistrationService
{
    Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request, CancellationToken cancellationToken = default);
    Task ShowBusinessOfOwnerAsync(string userId, CancellationToken cancellationToken = default);
    // The approval link BESTI gets by e-mail: null when the link is wrong
    Task<BusinessApprovalDto?> GetApprovalAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ApproveAsync(string token, CancellationToken cancellationToken = default);
}