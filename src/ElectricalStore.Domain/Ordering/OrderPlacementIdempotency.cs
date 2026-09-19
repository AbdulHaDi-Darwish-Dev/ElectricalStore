namespace ElectricalStore.Domain.Ordering;

/// <summary>
/// Place Order idempotency record. Guest bearer tokens are never stored raw —
/// Application supplies a protected payload for replay within the retention window.
/// </summary>
public sealed class OrderPlacementIdempotency
{
    public const int KeyHashMaxLength = 128;
    public const int ScopeMaxLength = 64;
    public const int ProtectedGuestTokenMaxLength = 1024;
    public const int RawKeyMinLength = 16;
    public const int RawKeyMaxLength = 128;
    public const int RetentionHours = 24;

    public Guid Id { get; private set; }

    /// <summary>SHA-256 hex of the client Idempotency-Key.</summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>"u:{userId}" for authenticated; "g" for guest.</summary>
    public string Scope { get; private set; } = string.Empty;

    public Guid OrderId { get; private set; }

    /// <summary>
    /// Application-protected guest access token for replay (null for authenticated orders).
    /// Never store the raw bearer token here.
    /// </summary>
    public string? ProtectedGuestAccessToken { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    private OrderPlacementIdempotency()
    {
    }

    public bool IsExpired(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC.", nameof(utcNow));

        return utcNow >= ExpiresAtUtc;
    }

    public static OrderPlacementIdempotency Create(
        string keyHash,
        string scope,
        Guid orderId,
        string? protectedGuestAccessToken,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new ArgumentException("Key hash is required.", nameof(keyHash));
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope is required.", nameof(scope));
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order id is required.", nameof(orderId));
        if (utcNow.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC.", nameof(utcNow));

        var hash = keyHash.Trim();
        if (hash.Length > KeyHashMaxLength)
            throw new ArgumentException($"Key hash must be {KeyHashMaxLength} characters or fewer.", nameof(keyHash));

        var scoped = scope.Trim();
        if (scoped.Length > ScopeMaxLength)
            throw new ArgumentException($"Scope must be {ScopeMaxLength} characters or fewer.", nameof(scope));

        string? protectedToken = null;
        if (!string.IsNullOrWhiteSpace(protectedGuestAccessToken))
        {
            protectedToken = protectedGuestAccessToken.Trim();
            if (protectedToken.Length > ProtectedGuestTokenMaxLength)
                throw new ArgumentException(
                    $"Protected guest token must be {ProtectedGuestTokenMaxLength} characters or fewer.",
                    nameof(protectedGuestAccessToken));
        }

        return new OrderPlacementIdempotency
        {
            Id = Guid.NewGuid(),
            KeyHash = hash,
            Scope = scoped,
            OrderId = orderId,
            ProtectedGuestAccessToken = protectedToken,
            CreatedAtUtc = utcNow,
            ExpiresAtUtc = utcNow.AddHours(RetentionHours)
        };
    }

    public static string ScopeForUser(Guid userId) => $"u:{userId:D}";

    public static string ScopeForGuest() => "g";
}
