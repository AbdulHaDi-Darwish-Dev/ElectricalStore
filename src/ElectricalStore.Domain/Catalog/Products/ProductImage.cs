namespace ElectricalStore.Domain.Catalog.Products;

public sealed class ProductImage
{
    public const int StorageKeyMaxLength = 256;
    public const int UrlMaxLength = 1024;

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public bool IsPrimary { get; private set; }

    public int SortOrder { get; private set; }

    private ProductImage()
    {
    }

    internal static ProductImage Create(
        Guid productId,
        string storageKey,
        string url,
        bool isPrimary,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Image storage key is required.", nameof(storageKey));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL is required.", nameof(url));

        var trimmedKey = storageKey.Trim();
        var trimmedUrl = url.Trim();
        if (trimmedKey.Length > StorageKeyMaxLength)
            throw new ArgumentException($"Image storage key must be {StorageKeyMaxLength} characters or fewer.", nameof(storageKey));
        if (trimmedUrl.Length > UrlMaxLength)
            throw new ArgumentException($"Image URL must be {UrlMaxLength} characters or fewer.", nameof(url));

        return new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            StorageKey = trimmedKey,
            Url = trimmedUrl,
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
    }

    internal void MarkPrimary() => IsPrimary = true;

    internal void ClearPrimary() => IsPrimary = false;

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
