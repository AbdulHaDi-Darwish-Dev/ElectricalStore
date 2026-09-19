using System.Security.Cryptography;

namespace ElectricalStore.IntegrationTests.Support;

/// <summary>Ephemeral RSA material for tests only — never use in production.</summary>
internal static class TestKeys
{
    static TestKeys()
    {
        using var rsa = RSA.Create(2048);
        PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
    }

    public static string PrivateKeyPem { get; }

    public static string PublicKeyPem { get; }

    public const string Issuer = "https://electricalstore.test";

    public const string Audience = "electricalstore-tests";

    public const string OwnerEmail = "owner@example.test";

    public const string OwnerUserName = "owner";

    public const string OwnerPassword = "Owner-Test-Password-1!";

    public const string UserPassword = "User-Test-Password-1!";
}
