using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Media;

namespace ElectricalStore.Application.Catalog.Categories;

public sealed class UpsertCategoryImageUseCase
{
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IImageStorage _storage;
    private readonly ImageUploadValidator _validator;

    public UpsertCategoryImageUseCase(
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork,
        IImageStorage storage,
        ImageUploadValidator validator)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _validator = validator;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(
        Guid categoryId,
        Stream content,
        string fileName,
        string contentType,
        long? declaredLength,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(content, contentType, declaredLength);
        if (validation.IsFailure)
            return Result.Failure<CategoryDto>(validation.Error!);

        var category = await _categories.GetTrackedByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryDto>(CategoryErrors.NotFound);

        var previousStorageKey = category.ImageStorageKey;
        StoredImage uploaded;
        try
        {
            uploaded = await _storage.UploadAsync(
                content,
                fileName,
                contentType,
                folder: "electricalstore/categories",
                cancellationToken);
        }
        catch (ImageStorageException ex)
        {
            return Result.Failure<CategoryDto>(ImageStorageFailureMapper.Map(ex));
        }

        try
        {
            category.SetImage(uploaded.StorageKey, uploaded.Url);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try { await _storage.DeleteAsync(uploaded.StorageKey, cancellationToken); }
            catch (ImageStorageException) { }

            return Result.Failure<CategoryDto>(MediaErrors.UploadFailed);
        }

        if (!string.IsNullOrWhiteSpace(previousStorageKey)
            && !string.Equals(previousStorageKey, uploaded.StorageKey, StringComparison.Ordinal))
        {
            try { await _storage.DeleteAsync(previousStorageKey, cancellationToken); }
            catch (ImageStorageException) { }
        }

        return Result.Success(CategoryDto.From(category));
    }
}

public sealed class DeleteCategoryImageUseCase
{
    private readonly ICategoryRepository _categories;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IImageStorage _storage;

    public DeleteCategoryImageUseCase(
        ICategoryRepository categories,
        IAppUnitOfWork unitOfWork,
        IImageStorage storage)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _storage = storage;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetTrackedByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryDto>(CategoryErrors.NotFound);

        if (!category.HasImage)
            return Result.Failure<CategoryDto>(CategoryErrors.ImageNotFound);

        var storageKey = category.ClearImage();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(storageKey))
        {
            try { await _storage.DeleteAsync(storageKey, cancellationToken); }
            catch (ImageStorageException) { }
        }

        return Result.Success(CategoryDto.From(category));
    }
}
