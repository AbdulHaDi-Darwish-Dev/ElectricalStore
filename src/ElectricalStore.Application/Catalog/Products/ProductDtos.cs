using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Catalog.Products;

public sealed record ProductVariantDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string SellingUnit,
    decimal QuantityIncrement,
    bool IsActive)
{
    public static ProductVariantDto From(ProductVariant variant) =>
        new(
            variant.Id,
            variant.Name,
            variant.Sku,
            variant.Price,
            variant.SellingUnit.ToString(),
            variant.QuantityIncrement,
            variant.IsActive);
}

public sealed record ProductImageDto(
    Guid Id,
    string Url,
    bool IsPrimary,
    int SortOrder)
{
    public static ProductImageDto From(ProductImage image) =>
        new(image.Id, image.Url, image.IsPrimary, image.SortOrder);
}

public sealed record ProductCategoryInfoDto(Guid Id, string Name, bool IsActive);

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    bool CategoryIsActive,
    bool IsActive,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images)
{
    public static ProductDto From(Product product, ProductCategoryInfoDto category) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.CategoryId,
            category.Name,
            category.IsActive,
            product.IsActive,
            product.Variants.Select(ProductVariantDto.From).OrderBy(v => v.Name).ToList(),
            product.Images.Select(ProductImageDto.From).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList());
}

public sealed record ProductListItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    bool IsActive,
    int VariantCount,
    string? PrimaryImageUrl,
    int ImageCount);

public sealed record CatalogProductListItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    decimal FromPrice,
    string PrimaryImageUrl,
    bool HasInStock);

/// <summary>
/// Public catalog variant including derived stock availability.
/// Missing inventory rows are treated as AvailableQuantity = 0 / IsInStock = false;
/// zero stock does not hide the active variant from the catalog.
/// </summary>
public sealed record CatalogProductVariantDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string SellingUnit,
    decimal QuantityIncrement,
    bool IsActive,
    decimal AvailableQuantity,
    bool IsInStock)
{
    public static CatalogProductVariantDto From(
        ProductVariantDto variant,
        decimal availableQuantity) =>
        new(
            variant.Id,
            variant.Name,
            variant.Sku,
            variant.Price,
            variant.SellingUnit,
            variant.QuantityIncrement,
            variant.IsActive,
            availableQuantity,
            availableQuantity > 0m);
}

public sealed record CatalogProductDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<CatalogProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images);

public sealed record CreateProductVariantRequest(
    string Name,
    string Sku,
    decimal Price,
    string SellingUnit,
    decimal QuantityIncrement,
    bool IsActive);

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    Guid CategoryId,
    bool IsActive,
    IReadOnlyList<CreateProductVariantRequest> Variants);

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    Guid CategoryId);

public sealed record UpdateProductVariantRequest(
    string Name,
    string Sku,
    decimal Price,
    string SellingUnit,
    decimal QuantityIncrement);

public sealed record ReorderProductImagesRequest(IReadOnlyList<Guid> OrderedImageIds);
