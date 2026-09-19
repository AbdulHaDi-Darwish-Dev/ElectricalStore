namespace ElectricalStore.Domain.Shipping;

/// <summary>
/// Fixed-fee Aleppo delivery zone (MVP). No distance/weight/carrier logic.
/// Future Place Order must snapshot Name + Fee onto the Order so later edits do not rewrite history.
/// </summary>
public sealed class DeliveryZone
{
    public const int NameMaxLength = 200;

    /// <summary>Same monetary precision as ProductVariant.Price (SYP MVP).</summary>
    public const int FeePrecision = 18;

    public const int FeeScale = 2;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Trimmed + upper-invariant form used for case-insensitive uniqueness.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public decimal Fee { get; private set; }

    public bool IsActive { get; private set; }

    private DeliveryZone()
    {
    }

    public static DeliveryZone Create(string name, decimal fee, bool isActive)
    {
        var (displayName, normalized) = NormalizeName(name);
        return new DeliveryZone
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            NormalizedName = normalized,
            Fee = NormalizeFee(fee),
            IsActive = isActive
        };
    }

    public void Update(string name, decimal fee)
    {
        var (displayName, normalized) = NormalizeName(name);
        Name = displayName;
        NormalizedName = normalized;
        Fee = NormalizeFee(fee);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public static string NormalizeNameKey(string name)
    {
        var (_, normalized) = NormalizeName(name);
        return normalized;
    }

    private static (string DisplayName, string Normalized) NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var displayName = name.Trim();
        if (displayName.Length > NameMaxLength)
            throw new ArgumentException($"Name must be {NameMaxLength} characters or fewer.", nameof(name));

        return (displayName, displayName.ToUpperInvariant());
    }

    private static decimal NormalizeFee(decimal fee)
    {
        if (fee < 0m)
            throw new ArgumentOutOfRangeException(nameof(fee), "Fee cannot be negative.");

        return decimal.Round(fee, FeeScale, MidpointRounding.AwayFromZero);
    }
}
