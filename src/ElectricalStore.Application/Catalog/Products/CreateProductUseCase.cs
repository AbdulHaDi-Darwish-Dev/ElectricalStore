using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Catalog.Products;

public sealed class CreateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public CreateProductUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var basics = ProductValidation.ValidateProductBasics(request.Name, request.Description, request.CategoryId);
        if (basics.IsFailure)
            return Result.Failure<ProductDto>(basics.Error!);

        if (request.Variants is null || request.Variants.Count == 0)
            return Result.Failure<ProductDto>(ProductErrors.VariantsRequired);

        var seeds = new List<ProductVariantSeed>(request.Variants.Count);
        foreach (var variantRequest in request.Variants)
        {
            var seedResult = ProductVariantRequestMapper.ToSeed(variantRequest);
            if (seedResult.IsFailure)
                return Result.Failure<ProductDto>(seedResult.Error!);
            seeds.Add(seedResult.Value);
        }

        try
        {
            Product.EnsureNoDuplicateVariantNames(seeds.Select(s => s.Name));
            Product.EnsureNoDuplicateSkus(seeds.Select(s => s.Sku));
        }
        catch (ArgumentException ex) when (ex.ParamName == "names")
        {
            return Result.Failure<ProductDto>(ProductErrors.DuplicateVariantName);
        }
        catch (ArgumentException ex) when (ex.ParamName == "skus")
        {
            return Result.Failure<ProductDto>(ProductErrors.DuplicateSkuInRequest);
        }

        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
            return Result.Failure<ProductDto>(ProductErrors.CategoryNotFound);

        var normalizedSkus = seeds.Select(s => ProductVariant.NormalizeSkuKey(s.Sku)).ToArray();
        if (await _products.AnyNormalizedSkuExistsAsync(normalizedSkus, excludeVariantId: null, cancellationToken))
            return Result.Failure<ProductDto>(ProductErrors.SkuAlreadyExists);

        Product product;
        try
        {
            product = Product.Create(
                request.Name,
                request.Description,
                request.CategoryId,
                request.IsActive,
                seeds);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProductDto>(ProductValidation.MapCreateException(ex));
        }

        await _products.AddAsync(product, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<ProductDto>(ProductErrors.SkuAlreadyExists);
        }

        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(category.Id, category.Name, category.IsActive)));
    }
}
