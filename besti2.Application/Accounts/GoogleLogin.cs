namespace besti2.Application.Accounts;

// "Fortsett med Google": the web (or later a mobile app) gets an ID token from Google and sends it here
public record GoogleLoginRequest(string IdToken);

// What Google says about the person, after the token's signature, audience and expiry are checked
public record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name);

public interface IGoogleTokenValidator
{
    // null when the token is not a valid Google ID token for BESTI
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

// UserId = the account to log in; otherwise Error is the message for the person
public record GoogleLoginResult(string? UserId, string? Error);

public interface IGoogleLoginService
{
    // Finds the account for the Google account (or its e-mail), or makes a new customer account
    Task<GoogleLoginResult> FindOrCreateUserAsync(string idToken, CancellationToken cancellationToken = default);
}
