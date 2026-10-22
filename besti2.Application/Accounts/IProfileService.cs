namespace besti2.Application.Accounts;

// The customer's own account. The methods return null when it worked, otherwise a message for the form
public interface IProfileService
{
    Task<ProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<string?> UpdateAsync(string userId, UpdateMyProfileRequest request, CancellationToken cancellationToken = default);
    Task<string?> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    // "Slett konto": future visits cancelled (the business is told), all visits anonymised, reviews kept as
    // "Tidligere kunde", notifications removed, the account deleted
    Task<string?> DeleteAsync(string userId, DeleteAccountRequest request, CancellationToken cancellationToken = default);
}