using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Categories;

public static class CategoryErrors
{
    public static Error NotFound { get; } =
        new("Category.NotFound", "Category was not found.");

    public static Error NameRequired { get; } =
        new("Category.NameRequired", "Category name is required.");

    public static Error NameTooLong { get; } =
        new("Category.NameTooLong", $"Category name must be {Domain.Catalog.Categories.Category.NameMaxLength} characters or fewer.");

    public static Error DescriptionTooLong { get; } =
        new("Category.DescriptionTooLong", $"Category description must be {Domain.Catalog.Categories.Category.DescriptionMaxLength} characters or fewer.");

    public static Error NameAlreadyExists { get; } =
        new("Category.NameAlreadyExists", "A category with this name already exists.");

    public static Error ImageNotFound { get; } =
        new("Category.ImageNotFound", "Category image was not found.");
}
