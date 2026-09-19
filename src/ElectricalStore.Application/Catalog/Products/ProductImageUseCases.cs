using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Media;
using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Catalog.Products;

public sealed class AddProductImageUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IImageStorage _storage;
    private readonly ImageUploadValidator _validator;

    public AddProductImageUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork,
        IImageStorage storage,
        ImageUploadValidator validator)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _validator = validator;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid productId,
        Stream content,
        string fileName,
        string contentType,
        long? declaredLength,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(content, contentType, declaredLength);
        if (validation.IsFailure)
            return Result.Failure<ProductDto>(validation.Error!);

        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        if (product.Images.Count >= Product.MaxImages)
            return Result.Failure<ProductDto>(ProductErrors.TooManyImages);

        StoredImage uploaded;
        try
        {
            uploaded = await _storage.UploadAsync(
                content,
                fileName,
                contentType,
                folder: "electricalstore/products",
                cancellationToken);
        }
        catch (ImageStorageException ex)
        {
            return Result.Failure<ProductDto>(ImageStorageFailureMapper.Map(ex));
        }

        try
        {
            product.AddImage(uploaded.StorageKey, uploaded.Url);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            try { await _storage.DeleteAsync(uploaded.StorageKey, cancellationToken); }
            catch (ImageStorageException) { }

            return Result.Failure<ProductDto>(ProductErrors.TooManyImages);
        }
        catch
        {
            try { await _storage.DeleteAsync(uploaded.StorageKey, cancellationToken); }
            catch (ImageStorageException) { }

            return Result.Failure<ProductDto>(MediaErrors.UploadFailed);
        }

        return await ToDtoAsync(product, cancellationToken);
    }

    private async Task<Result<ProductDto>> ToDtoAsync(Product product, CancellationToken cancellationToken)
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

public sealed class DeleteProductImageUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IImageStorage _storage;

    public DeleteProductImageUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork,
        IImageStorage storage)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _storage = storage;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        string storageKey;
        try
        {
            storageKey = product.RemoveImage(imageId);
        }
        catch (ArgumentException)
        {
            return Result.Failure<ProductDto>(ProductErrors.ImageNotFound);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try { await _storage.DeleteAsync(storageKey, cancellationToken); }
        catch (ImageStorageException) { }

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        return Result.Success(ProductDto.From(
            product,
            new ProductCategoryInfoDto(
                product.CategoryId,
                category?.Name ?? string.Empty,
                category?.IsActive ?? false)));
    }
}

public sealed class SetPrimaryProductImageUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public SetPrimaryProductImageUseCase(
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
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        try
        {
            product.SetPrimaryImage(imageId);
        }
        catch (ArgumentException)
        {
            return Result.Failure<ProductDto>(ProductErrors.ImageNotFound);
        }

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

public sealed class ReorderProductImagesUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;

    public ReorderProductImagesUseCase(
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
        IReadOnlyList<Guid> orderedImageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedImageIds);

        var product = await _products.GetTrackedByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(ProductErrors.NotFound);

        try
        {
            product.ReorderImages(orderedImageIds);
        }
        catch (ArgumentException)
        {
            return Result.Failure<ProductDto>(ProductErrors.InvalidImageReorder);
        }

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
