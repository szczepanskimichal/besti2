namespace besti2.Application.Accounts;

// "Min profil" in Min Besti
// HasPassword = false for an account made with "Fortsett med Google" (it can make a password under Profil)
public record ProfileDto(string FullName, string Email, string? Phone, bool HasPassword = true);

public record UpdateMyProfileRequest(string FullName, string? Phone);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record DeleteAccountRequest(string Password);