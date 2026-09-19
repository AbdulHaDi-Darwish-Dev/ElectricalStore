using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Products;

public sealed class GetAdminProductByIdUseCase
{
    private readonly IProductRepository _products;

    public GetAdminProductByIdUseCase(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var loaded = await _products.GetAdminByIdAsync(productId, cancellationToken);
        if (loaded is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        var (product, categoryName, categoryIsActive) = loaded.Value;
        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(product.CategoryId, categoryName, categoryIsActive)));
    }
}

public sealed class ListAdminProductsUseCase
{
    private readonly IProductRepository _products;

    public ListAdminProductsUseCase(IProductRepository products)
    {
        _products = products;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> ExecuteAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await _products.ListAdminAsync(filter, cancellationToken);
        return rows
            .Select(r =>
            {
                var primary = r.Product.Images.FirstOrDefault(i => i.IsPrimary)
                              ?? r.Product.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
                return new ProductListItemDto(
                    r.Product.Id,
                    r.Product.Name,
                    r.Product.Description,
                    r.Product.CategoryId,
                    r.CategoryName,
                    r.Product.IsActive,
                    r.Product.Variants.Count,
                    primary?.Url,
                    r.Product.Images.Count);
            })
            .ToList();
    }
}

public sealed class GetCatalogProductByIdUseCase
{
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public GetCatalogProductByIdUseCase(
        IProductRepository products,
        IInventoryRepository inventory)
    {
        _products = products;
        _inventory = inventory;
    }

    public async Task<Result<CatalogProductDto>> ExecuteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var loaded = await _products.GetCatalogByIdAsync(productId, cancellationToken);
        if (loaded is null)
            return Result.Failure<CatalogProductDto>(ProductErrors.NotFound);

        var (product, categoryName) = loaded.Value;
        var activeVariants = product.Variants
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(ProductVariantDto.From)
            .ToList();

        if (activeVariants.Count == 0 || !product.HasImages)
            return Result.Failure<CatalogProductDto>(ProductErrors.NotFound);

        var availability = await _inventory.GetAvailabilityMapAsync(
            activeVariants.Select(v => v.Id).ToList(),
            cancellationToken);

        var catalogVariants = activeVariants
            .Select(v =>
            {
                var available = availability.TryGetValue(v.Id, out var stock)
                    ? stock.Available
                    : 0m;
                return CatalogProductVariantDto.From(v, available);
            })
            .ToList();

        var images = product.Images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(ProductImageDto.From)
            .ToList();

        return Result.Success(new CatalogProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.CategoryId,
            categoryName,
            catalogVariants,
            images));
    }
}

public sealed class ListCatalogProductsUseCase
{
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public ListCatalogProductsUseCase(
        IProductRepository products,
        IInventoryRepository inventory)
    {
        _products = products;
        _inventory = inventory;
    }

    public async Task<IReadOnlyList<CatalogProductListItemDto>> ExecuteAsync(
        CatalogProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await _products.ListCatalogAsync(filter, cancellationToken);
        var variantIds = rows
            .SelectMany(r => r.Product.Variants.Where(v => v.IsActive).Select(v => v.Id))
            .Distinct()
            .ToList();
        var availability = await _inventory.GetAvailabilityMapAsync(variantIds, cancellationToken);

        return rows
            .Select(r =>
            {
                var activeVariants = r.Product.Variants.Where(v => v.IsActive).ToList();
                var primary = r.Product.Images.First(i => i.IsPrimary);
                var hasInStock = activeVariants.Any(v =>
                    availability.TryGetValue(v.Id, out var stock) && stock.Available > 0m);
                return new CatalogProductListItemDto(
                    r.Product.Id,
                    r.Product.Name,
                    r.Product.Description,
                    r.Product.CategoryId,
                    r.CategoryName,
                    activeVariants.Min(v => v.Price),
                    primary.Url,
                    hasInStock);
            })
            .ToList();
    }
}
