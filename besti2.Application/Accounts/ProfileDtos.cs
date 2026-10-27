namespace besti2.Application.Accounts;

// "Min profil" in Min Besti
public record ProfileDto(string FullName, string Email, string? Phone);

public record UpdateMyProfileRequest(string FullName, string? Phone);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record DeleteAccountRequest(string Password);