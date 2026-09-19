using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Abstractions;

public sealed record ProductListFilter(
    Guid? CategoryId = null,
    bool? IsActive = null,
    string? Search = null);

public sealed record CatalogProductListFilter(
    Guid? CategoryId = null,
    string? Search = null);

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Set-based check: returns true if any of the normalized SKUs already exist (optionally excluding a variant).</summary>
    Task<bool> AnyNormalizedSkuExistsAsync(
        IReadOnlyCollection<string> normalizedSkus,
        Guid? excludeVariantId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Product Product, string CategoryName, bool CategoryIsActive)>> ListAdminAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default);

    Task<(Product Product, string CategoryName, bool CategoryIsActive)?> GetAdminByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Product Product, string CategoryName)>> ListCatalogAsync(
        CatalogProductListFilter filter,
        CancellationToken cancellationToken = default);

    Task<(Product Product, string CategoryName)?> GetCatalogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
