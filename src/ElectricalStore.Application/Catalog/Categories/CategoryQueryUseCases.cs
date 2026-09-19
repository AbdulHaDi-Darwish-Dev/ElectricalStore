using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Categories;

public sealed class GetAdminCategoryByIdUseCase
{
    private readonly ICategoryRepository _repository;

    public GetAdminCategoryByIdUseCase(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        return category is null
            ? Result.Failure<CategoryDto>(CategoryErrors.NotFound)
            : Result.Success(CategoryDto.From(category));
    }
}

public sealed class ListAdminCategoriesUseCase
{
    private readonly ICategoryRepository _repository;

    public ListAdminCategoriesUseCase(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CategoryDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.ListAsync(activeOnly: false, requireImage: false, cancellationToken);
        return categories.Select(CategoryDto.From).ToList();
    }
}

public sealed class GetActiveCategoryByIdUseCase
{
    private readonly ICategoryRepository _repository;

    public GetActiveCategoryByIdUseCase(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null || !category.IsActive || !category.HasImage)
            return Result.Failure<CategoryDto>(CategoryErrors.NotFound);

        return Result.Success(CategoryDto.From(category));
    }
}

public sealed class ListActiveCategoriesUseCase
{
    private readonly ICategoryRepository _repository;

    public ListActiveCategoriesUseCase(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CategoryDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.ListAsync(activeOnly: true, requireImage: true, cancellationToken);
        return categories.Select(CategoryDto.From).ToList();
    }
}
