using besti2.Application.Accounts;
using Google.Apis.Auth;

namespace besti2.Infrastructure.Identity;

// Checks Google's signature (Google's public keys), that the token was made for BESTI's client id and
// that it has not expired. clientId empty = Google login is off
public class GoogleTokenValidator(string? clientId) : IGoogleTokenValidator
{
    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrWhiteSpace(idToken) || idToken.Length > 4096)
        {
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
            return payload.Email is null
                ? null
                : new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.Name);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
