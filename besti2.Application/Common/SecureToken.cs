using System.Security.Cryptography;
using System.Text;

namespace besti2.Application.Common;

public static class SecureToken
{
    // A one-time link token: 32 random bytes, URL-safe; only its hash is stored
    public static string New() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}