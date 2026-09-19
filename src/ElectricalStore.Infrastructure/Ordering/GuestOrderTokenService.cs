using System.Security.Cryptography;
using System.Text;
using ElectricalStore.Application.Abstractions;

namespace ElectricalStore.Infrastructure.Ordering;

/// <summary>
/// Opaque guest order tokens: 32-byte CSPRNG, Base64Url for clients, SHA-256 hex in DB.
/// </summary>
public sealed class GuestOrderTokenService : IGuestOrderTokenService
{
    public (string RawToken, string TokenHash) CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        var raw = Base64UrlEncode(bytes);
        return (raw, HashToken(raw));
    }

    public string HashToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new ArgumentException("Token is required.", nameof(rawToken));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(hash);
    }

    public bool TokensMatch(string rawToken, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        string computed;
        try
        {
            computed = HashToken(rawToken);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(computed);
        var b = Encoding.UTF8.GetBytes(storedHash.Trim().ToUpperInvariant());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
