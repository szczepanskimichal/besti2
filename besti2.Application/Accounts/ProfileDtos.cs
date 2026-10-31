namespace besti2.Application.Accounts;

// "Min profil" in Min Besti
// HasPassword = false for an account made with "Fortsett med Google" (it can make a password under Profil)
// ReminderEmail / SoonNotice: the "Varsler" choices (reminder e-mail the day before, "Snart tid!" 2 hours before)
// CreatedUtc: "Din besti siden oktober 2026" (null for accounts made before it was saved)
public record ProfileDto(string FullName, string Email, string? Phone, bool HasPassword = true,
    bool ReminderEmail = true, bool SoonNotice = true, DateTime? CreatedUtc = null);

public record NotificationSettingsRequest(bool ReminderEmail, bool SoonNotice);

public record UpdateMyProfileRequest(string FullName, string? Phone);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record DeleteAccountRequest(string Password);