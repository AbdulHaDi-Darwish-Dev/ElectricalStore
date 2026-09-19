using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Catalog.Categories;

namespace ElectricalStore.Application.Catalog.Categories;

public sealed class UpdateCategoryUseCase
{
    private readonly ICategoryRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public UpdateCategoryUseCase(ICategoryRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = CreateCategoryUseCase.ValidateNameAndDescription(request.Name, request.Description);
        if (validation.IsFailure)
            return Result.Failure<CategoryDto>(validation.Error!);

        var category = await _repository.GetTrackedByIdAsync(id, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryDto>(CategoryErrors.NotFound);

        var normalized = Category.NormalizeNameKey(request.Name);
        if (await _repository.ExistsByNormalizedNameAsync(normalized, excludeId: id, cancellationToken))
            return Result.Failure<CategoryDto>(CategoryErrors.NameAlreadyExists);

        try
        {
            category.Update(request.Name, request.Description);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CategoryDto>(CategoryErrors.NameRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name")
        {
            return Result.Failure<CategoryDto>(CategoryErrors.NameTooLong);
        }
        catch (ArgumentException ex) when (ex.ParamName == "description")
        {
            return Result.Failure<CategoryDto>(CategoryErrors.DescriptionTooLong);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<CategoryDto>(CategoryErrors.NameAlreadyExists);
        }

        return Result.Success(CategoryDto.From(category));
    }
}
