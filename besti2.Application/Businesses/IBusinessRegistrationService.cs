namespace besti2.Application.Businesses;

public interface IBusinessRegistrationService
{
    Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request,
        CancellationToken cancellationToken = default);

    Task ShowBusinessOfOwnerAsync(string userId, CancellationToken cancellationToken = default);

    // The approval link BESTI gets by e-mail: null when the link is wrong
    Task<BusinessApprovalDto?> GetApprovalAsync(string token, CancellationToken cancellationToken = default);

    Task<bool> ApproveAsync(string token, CancellationToken cancellationToken = default);

    // Deletes a business that is not approved yet, with its owner account
    Task<bool> RejectAsync(string token, CancellationToken cancellationToken = default);

    // The same two from the admin page; false when the business does not wait for approval
    Task<bool> ApproveByIdAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task<bool> RejectByIdAsync(Guid businessId, CancellationToken cancellationToken = default);

    // "Slett bedrift": the owner deletes the business with all its data; null = deleted, otherwise the error
    Task<string?> DeleteOwnBusinessAsync(string userId, string? password, CancellationToken cancellationToken = default);
}