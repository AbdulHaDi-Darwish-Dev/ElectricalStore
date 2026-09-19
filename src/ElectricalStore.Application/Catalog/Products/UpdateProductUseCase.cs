using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Products;

public sealed class UpdateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public UpdateProductUseCase(
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
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var basics = ProductValidation.ValidateProductBasics(request.Name, request.Description, request.CategoryId);
        if (basics.IsFailure)
            return Result.Failure<ProductDto>(basics.Error!);

        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
            return Result.Failure<ProductDto>(ProductErrors.CategoryNotFound);

        try
        {
            product.Update(request.Name, request.Description, request.CategoryId);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductDto>(ProductValidation.MapCreateException(ex));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(category.Id, category.Name, category.IsActive)));
    }
}
