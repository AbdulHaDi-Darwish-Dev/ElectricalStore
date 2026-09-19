using System.Security.Cryptography;
using ElectricalStore.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace ElectricalStore.Infrastructure.Ordering;

/// <summary>
/// Protects guest order bearer tokens for idempotency replay using ASP.NET Core Data Protection.
/// </summary>
public sealed class DataProtectionGuestAccessTokenProtector : IGuestAccessTokenProtector
{
    private const string Purpose = "ElectricalStore.Ordering.GuestAccessToken.v1";

    private readonly IDataProtector _protector;

    public DataProtectionGuestAccessTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string rawGuestAccessToken)
    {
        if (string.IsNullOrWhiteSpace(rawGuestAccessToken))
            throw new ArgumentException("Guest access token is required.", nameof(rawGuestAccessToken));

        return _protector.Protect(rawGuestAccessToken.Trim());
    }

    public string? Unprotect(string protectedGuestAccessToken)
    {
        if (string.IsNullOrWhiteSpace(protectedGuestAccessToken))
            return null;

        try
        {
            return _protector.Unprotect(protectedGuestAccessToken.Trim());
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
