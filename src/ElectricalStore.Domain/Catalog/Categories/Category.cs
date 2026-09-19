namespace ElectricalStore.Domain.Catalog.Categories;

public sealed class Category
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int ImageStorageKeyMaxLength = 256;
    public const int ImageUrlMaxLength = 1024;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Trimmed + upper-invariant form used for case-insensitive uniqueness.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Provider-neutral storage key when an image is assigned; null when absent.</summary>
    public string? ImageStorageKey { get; private set; }

    /// <summary>Deliverable image URL for clients.</summary>
    public string? ImageUrl { get; private set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageStorageKey);

    private Category()
    {
    }

    public static Category Create(string name, string? description, bool isActive)
    {
        var (displayName, normalized) = NormalizeName(name);
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            NormalizedName = normalized,
            Description = NormalizeDescription(description),
            IsActive = isActive
        };
    }

    public void Update(string name, string? description)
    {
        var (displayName, normalized) = NormalizeName(name);
        Name = displayName;
        NormalizedName = normalized;
        Description = NormalizeDescription(description);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetImage(string storageKey, string url)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Image storage key is required.", nameof(storageKey));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL is required.", nameof(url));

        var trimmedKey = storageKey.Trim();
        var trimmedUrl = url.Trim();
        if (trimmedKey.Length > ImageStorageKeyMaxLength)
            throw new ArgumentException($"Image storage key must be {ImageStorageKeyMaxLength} characters or fewer.", nameof(storageKey));
        if (trimmedUrl.Length > ImageUrlMaxLength)
            throw new ArgumentException($"Image URL must be {ImageUrlMaxLength} characters or fewer.", nameof(url));

        ImageStorageKey = trimmedKey;
        ImageUrl = trimmedUrl;
    }

    public string? ClearImage()
    {
        var previous = ImageStorageKey;
        ImageStorageKey = null;
        ImageUrl = null;
        return previous;
    }

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
