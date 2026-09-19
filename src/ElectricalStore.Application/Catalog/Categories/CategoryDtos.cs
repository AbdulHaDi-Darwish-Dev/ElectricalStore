using ElectricalStore.Domain.Catalog.Categories;

namespace ElectricalStore.Application.Catalog.Categories;

public sealed class CategoryDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required bool IsActive { get; init; }

    public string? ImageUrl { get; init; }

    public bool HasImage { get; init; }

    public static CategoryDto From(Category category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ImageUrl = category.ImageUrl,
            HasImage = category.HasImage
        };
}

public sealed class CreateCategoryRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required bool IsActive { get; init; }
}

public sealed class UpdateCategoryRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
