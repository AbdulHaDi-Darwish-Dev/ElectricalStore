namespace ElectricalStore.Domain.Catalog.Products;

public sealed class ProductVariant
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 64;

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Trimmed + upper-invariant form for case-insensitive uniqueness within a product.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    /// <summary>Trimmed + upper-invariant form for global case-insensitive SKU uniqueness.</summary>
    public string NormalizedSku { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public SellingUnit SellingUnit { get; private set; }

    public decimal QuantityIncrement { get; private set; }

    public bool IsActive { get; private set; }

    private ProductVariant()
    {
    }

    internal static ProductVariant Create(
        Guid productId,
        string name,
        string sku,
        decimal price,
        SellingUnit sellingUnit,
        decimal quantityIncrement,
        bool isActive)
    {
        var (displayName, normalizedName) = NormalizeName(name);
        var (displaySku, normalizedSku) = NormalizeSku(sku);
        ValidatePricingAndIncrement(price, sellingUnit, quantityIncrement);

        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = displayName,
            NormalizedName = normalizedName,
            Sku = displaySku,
            NormalizedSku = normalizedSku,
            Price = price,
            SellingUnit = sellingUnit,
            QuantityIncrement = quantityIncrement,
            IsActive = isActive
        };
    }

    internal void Update(
        string name,
        string sku,
        decimal price,
        SellingUnit sellingUnit,
        decimal quantityIncrement)
    {
        var (displayName, normalizedName) = NormalizeName(name);
        var (displaySku, normalizedSku) = NormalizeSku(sku);
        ValidatePricingAndIncrement(price, sellingUnit, quantityIncrement);

        Name = displayName;
        NormalizedName = normalizedName;
        Sku = displaySku;
        NormalizedSku = normalizedSku;
        Price = price;
        SellingUnit = sellingUnit;
        QuantityIncrement = quantityIncrement;
    }

    internal void Activate() => IsActive = true;

    internal void Deactivate() => IsActive = false;

    public static string NormalizeNameKey(string name)
    {
        var (_, normalized) = NormalizeName(name);
        return normalized;
    }

    public static string NormalizeSkuKey(string sku)
    {
        var (_, normalized) = NormalizeSku(sku);
        return normalized;
    }

    private static (string DisplayName, string Normalized) NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name is required.", nameof(name));

        var displayName = name.Trim();
        if (displayName.Length > NameMaxLength)
            throw new ArgumentException($"Variant name must be {NameMaxLength} characters or fewer.", nameof(name));

        return (displayName, displayName.ToUpperInvariant());
    }

    private static (string DisplaySku, string Normalized) NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));

        var displaySku = sku.Trim();
        if (displaySku.Length > SkuMaxLength)
            throw new ArgumentException($"SKU must be {SkuMaxLength} characters or fewer.", nameof(sku));

        return (displaySku, displaySku.ToUpperInvariant());
    }

    private static void ValidatePricingAndIncrement(
        decimal price,
        SellingUnit sellingUnit,
        decimal quantityIncrement)
    {
        if (price <= 0m)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        if (quantityIncrement <= 0m)
            throw new ArgumentException("Quantity increment must be greater than zero.", nameof(quantityIncrement));

        if (sellingUnit == SellingUnit.Piece && quantityIncrement != 1m)
            throw new ArgumentException(
                "Piece selling unit requires quantity increment of exactly 1.",
                nameof(quantityIncrement));

        if (sellingUnit is not (SellingUnit.Piece or SellingUnit.Meter))
            throw new ArgumentOutOfRangeException(nameof(sellingUnit), "Unsupported selling unit.");
    }
}
