using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Products;

public sealed class ActivateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public ActivateProductUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        product.Activate();
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

public sealed class DeactivateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public DeactivateProductUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        product.Deactivate();
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
