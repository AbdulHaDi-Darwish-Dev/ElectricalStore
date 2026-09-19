namespace ElectricalStore.Domain.Catalog.Products;

public sealed class Product
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int MaxImages = 4;

    private readonly List<ProductVariant> _variants = new();
    private readonly List<ProductImage> _images = new();

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public bool HasImages => _images.Count > 0;

    private Product()
    {
    }

    public static Product Create(
        string name,
        string? description,
        Guid categoryId,
        bool isActive,
        IReadOnlyList<ProductVariantSeed> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category is required.", nameof(categoryId));

        if (variants.Count == 0)
            throw new ArgumentException("At least one variant is required.", nameof(variants));

        var (displayName, _) = NormalizeName(name);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            Description = NormalizeDescription(description),
            CategoryId = categoryId,
            IsActive = isActive
        };

        EnsureNoDuplicateVariantNames(variants.Select(v => v.Name));
        EnsureNoDuplicateSkus(variants.Select(v => v.Sku));

        foreach (var seed in variants)
        {
            product._variants.Add(ProductVariant.Create(
                product.Id,
                seed.Name,
                seed.Sku,
                seed.Price,
                seed.SellingUnit,
                seed.QuantityIncrement,
                seed.IsActive));
        }

        return product;
    }

    public void Update(string name, string? description, Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category is required.", nameof(categoryId));

        var (displayName, _) = NormalizeName(name);
        Name = displayName;
        Description = NormalizeDescription(description);
        CategoryId = categoryId;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public ProductVariant AddVariant(ProductVariantSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var normalizedName = ProductVariant.NormalizeNameKey(seed.Name);
        if (_variants.Any(v => v.NormalizedName == normalizedName))
            throw new ArgumentException("Variant name must be unique within the product.", nameof(seed));

        var normalizedSku = ProductVariant.NormalizeSkuKey(seed.Sku);
        if (_variants.Any(v => v.NormalizedSku == normalizedSku))
            throw new ArgumentException("SKU must be unique.", nameof(seed));

        var variant = ProductVariant.Create(
            Id,
            seed.Name,
            seed.Sku,
            seed.Price,
            seed.SellingUnit,
            seed.QuantityIncrement,
            seed.IsActive);
        _variants.Add(variant);
        return variant;
    }

    public ProductVariant UpdateVariant(Guid variantId, ProductVariantSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var variant = GetVariantOrThrow(variantId);

        var normalizedName = ProductVariant.NormalizeNameKey(seed.Name);
        if (_variants.Any(v => v.Id != variantId && v.NormalizedName == normalizedName))
            throw new ArgumentException("Variant name must be unique within the product.", nameof(seed));

        var normalizedSku = ProductVariant.NormalizeSkuKey(seed.Sku);
        if (_variants.Any(v => v.Id != variantId && v.NormalizedSku == normalizedSku))
            throw new ArgumentException("SKU must be unique.", nameof(seed));

        variant.Update(seed.Name, seed.Sku, seed.Price, seed.SellingUnit, seed.QuantityIncrement);
        return variant;
    }

    public ProductVariant ActivateVariant(Guid variantId)
    {
        var variant = GetVariantOrThrow(variantId);
        variant.Activate();
        return variant;
    }

    public ProductVariant DeactivateVariant(Guid variantId)
    {
        var variant = GetVariantOrThrow(variantId);
        variant.Deactivate();
        return variant;
    }

    public ProductVariant GetVariantOrThrow(Guid variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId);
        if (variant is null)
            throw new ArgumentException("Variant was not found.", nameof(variantId));
        return variant;
    }

    public ProductImage AddImage(string storageKey, string url)
    {
        if (_images.Count >= MaxImages)
            throw new InvalidOperationException("A product may have at most 4 images.");

        var sortOrder = _images.Count == 0 ? 0 : _images.Max(i => i.SortOrder) + 1;
        var isPrimary = _images.Count == 0;
        var image = ProductImage.Create(Id, storageKey, url, isPrimary, sortOrder);
        _images.Add(image);
        return image;
    }

    public string RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new ArgumentException("Image was not found.", nameof(imageId));

        var storageKey = image.StorageKey;
        var wasPrimary = image.IsPrimary;
        _images.Remove(image);

        if (wasPrimary && _images.Count > 0)
        {
            var next = _images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).First();
            next.MarkPrimary();
        }

        ReindexSortOrders();
        return storageKey;
    }

    public ProductImage SetPrimaryImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new ArgumentException("Image was not found.", nameof(imageId));

        foreach (var existing in _images)
            existing.ClearPrimary();

        image.MarkPrimary();
        return image;
    }

    public void ReorderImages(IReadOnlyList<Guid> orderedImageIds)
    {
        ArgumentNullException.ThrowIfNull(orderedImageIds);

        if (orderedImageIds.Count != _images.Count
            || orderedImageIds.Distinct().Count() != orderedImageIds.Count
            || orderedImageIds.Any(id => _images.All(i => i.Id != id)))
        {
            throw new ArgumentException(
                "Reorder must include each existing image id exactly once.",
                nameof(orderedImageIds));
        }

        for (var i = 0; i < orderedImageIds.Count; i++)
        {
            var image = _images.First(x => x.Id == orderedImageIds[i]);
            image.SetSortOrder(i);
        }
    }

    public static void EnsureNoDuplicateVariantNames(IEnumerable<string> names)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var key = ProductVariant.NormalizeNameKey(name);
            if (!seen.Add(key))
                throw new ArgumentException("Variant names must be unique within the product (case-insensitive).", nameof(names));
        }
    }

    public static void EnsureNoDuplicateSkus(IEnumerable<string> skus)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sku in skus)
        {
            var key = ProductVariant.NormalizeSkuKey(sku);
            if (!seen.Add(key))
                throw new ArgumentException("SKUs must be unique (case-insensitive).", nameof(skus));
        }
    }

    private void ReindexSortOrders()
    {
        var ordered = _images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].SetSortOrder(i);
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

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
            throw new ArgumentException(
                $"Description must be {DescriptionMaxLength} characters or fewer.",
                nameof(description));

        return trimmed;
    }
}

/// <summary>Input values used when creating or updating a variant inside the Product aggregate.</summary>
public sealed record ProductVariantSeed(
    string Name,
    string Sku,
    decimal Price,
    SellingUnit SellingUnit,
    decimal QuantityIncrement,
    bool IsActive);
