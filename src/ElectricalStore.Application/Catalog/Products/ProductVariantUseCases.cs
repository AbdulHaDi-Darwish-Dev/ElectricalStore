using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Catalog.Products;

public sealed class AddProductVariantUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public AddProductVariantUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var seedResult = ProductVariantRequestMapper.ToSeed(request);
        if (seedResult.IsFailure)
            return Result.Failure<ProductDto>(seedResult.Error!);

        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        var normalizedSku = ProductVariant.NormalizeSkuKey(seedResult.Value.Sku);
        if (await _products.AnyNormalizedSkuExistsAsync([normalizedSku], excludeVariantId: null, cancellationToken))
            return Result.Failure<ProductDto>(ProductErrors.SkuAlreadyExists);

        try
        {
            product.AddVariant(seedResult.Value);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductDto>(ProductValidation.MapVariantMutationException(ex));
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<ProductDto>(ProductErrors.SkuAlreadyExists);
        }

        return await ToDtoAsync(product, cancellationToken);
    }

    private async Task<Result<ProductDto>> ToDtoAsync(Domain.Catalog.Products.Product product, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(
                product.CategoryId,
                category?.Name ?? string.Empty,
                category?.IsActive ?? false)));
    }
}

public sealed class UpdateProductVariantUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public UpdateProductVariantUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        var existing = product.Variants.FirstOrDefault(v => v.Id == variantId);
        if (existing is null)
            return Result.Failure<ProductDto>(ProductErrors.VariantNotFound);

        var seedResult = ProductVariantRequestMapper.ToSeed(request, existing.IsActive);
        if (seedResult.IsFailure)
            return Result.Failure<ProductDto>(seedResult.Error!);

        var normalizedSku = ProductVariant.NormalizeSkuKey(seedResult.Value.Sku);
        if (await _products.AnyNormalizedSkuExistsAsync([normalizedSku], excludeVariantId: variantId, cancellationToken))
            return Result.Failure<ProductDto>(ProductErrors.SkuAlreadyExists);

        try
        {
            product.UpdateVariant(variantId, seedResult.Value);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductDto>(ProductValidation.MapVariantMutationException(ex));
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<ProductDto>(ProductErrors.SkuAlreadyExists);
        }

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(
                product.CategoryId,
                category?.Name ?? string.Empty,
                category?.IsActive ?? false)));
    }
}

public sealed class ActivateProductVariantUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public ActivateProductVariantUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        if (product.Variants.All(v => v.Id != variantId))
            return Result.Failure<ProductDto>(ProductErrors.VariantNotFound);

        product.ActivateVariant(variantId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(
                product.CategoryId,
                category?.Name ?? string.Empty,
                category?.IsActive ?? false)));
    }
}

public sealed class DeactivateProductVariantUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public DeactivateProductVariantUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        if (product.Variants.All(v => v.Id != variantId))
            return Result.Failure<ProductDto>(ProductErrors.VariantNotFound);

        product.DeactivateVariant(variantId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(
                product.CategoryId,
                category?.Name ?? string.Empty,
                category?.IsActive ?? false)));
    }
}
