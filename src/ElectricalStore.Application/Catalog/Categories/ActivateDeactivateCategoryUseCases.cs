using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Categories;

public sealed class ActivateCategoryUseCase
{
    private readonly ICategoryRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public ActivateCategoryUseCase(ICategoryRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetTrackedByIdAsync(id, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryDto>(CategoryErrors.NotFound);

        category.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CategoryDto.From(category));
    }
}

public sealed class DeactivateCategoryUseCase
{
    private readonly ICategoryRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public DeactivateCategoryUseCase(ICategoryRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetTrackedByIdAsync(id, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryDto>(CategoryErrors.NotFound);

        category.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CategoryDto.From(category));
    }
}
