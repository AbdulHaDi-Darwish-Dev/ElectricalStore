using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Catalog.Categories;

namespace ElectricalStore.Application.Catalog.Categories;

public sealed class CreateCategoryUseCase
{
    private readonly ICategoryRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public CreateCategoryUseCase(ICategoryRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> ExecuteAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateNameAndDescription(request.Name, request.Description);
        if (validation.IsFailure)
            return Result.Failure<CategoryDto>(validation.Error!);

        var normalized = Category.NormalizeNameKey(request.Name);
        if (await _repository.ExistsByNormalizedNameAsync(normalized, excludeId: null, cancellationToken))
            return Result.Failure<CategoryDto>(CategoryErrors.NameAlreadyExists);

        Category category;
        try
        {
            category = Category.Create(request.Name, request.Description, request.IsActive);
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

        await _repository.AddAsync(category, cancellationToken);

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

    internal static Result ValidateNameAndDescription(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(CategoryErrors.NameRequired);

        if (name.Trim().Length > Category.NameMaxLength)
            return Result.Failure(CategoryErrors.NameTooLong);

        if (description is not null
            && !string.IsNullOrWhiteSpace(description)
            && description.Trim().Length > Category.DescriptionMaxLength)
        {
            return Result.Failure(CategoryErrors.DescriptionTooLong);
        }

        return Result.Success();
    }
}
