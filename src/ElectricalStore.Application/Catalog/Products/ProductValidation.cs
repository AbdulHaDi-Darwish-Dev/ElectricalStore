using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Catalog.Products;

internal static class ProductValidation
{
    public static Result ValidateProductBasics(string? name, string? description, Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            return Result.Failure(ProductErrors.CategoryRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ProductErrors.NameRequired);

        if (name.Trim().Length > Product.NameMaxLength)
            return Result.Failure(ProductErrors.NameTooLong);

        if (description is not null
            && !string.IsNullOrWhiteSpace(description)
            && description.Trim().Length > Product.DescriptionMaxLength)
        {
            return Result.Failure(ProductErrors.DescriptionTooLong);
        }

        return Result.Success();
    }

    public static Error MapCreateException(ArgumentException ex) =>
        ex.ParamName switch
        {
            "categoryId" => ProductErrors.CategoryRequired,
            "name" when ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) => ProductErrors.NameRequired,
            "name" => ProductErrors.NameTooLong,
            "description" => ProductErrors.DescriptionTooLong,
            "variants" => ProductErrors.VariantsRequired,
            "names" => ProductErrors.DuplicateVariantName,
            "skus" => ProductErrors.DuplicateSkuInRequest,
            "price" => ProductErrors.InvalidPrice,
            "quantityIncrement" => ProductErrors.InvalidQuantityIncrement,
            "sellingUnit" => ProductErrors.InvalidSellingUnit,
            "sku" when ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) => ProductErrors.SkuRequired,
            "sku" => ProductErrors.SkuTooLong,
            _ when ex.Message.Contains("Variant name", StringComparison.OrdinalIgnoreCase)
                && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) => ProductErrors.VariantNameRequired,
            _ => ProductErrors.NameRequired
        };

    public static Error MapVariantMutationException(ArgumentException ex) =>
        ex.ParamName switch
        {
            "variantId" => ProductErrors.VariantNotFound,
            "seed" when ex.Message.Contains("Variant name", StringComparison.OrdinalIgnoreCase) => ProductErrors.DuplicateVariantName,
            "seed" when ex.Message.Contains("SKU", StringComparison.OrdinalIgnoreCase) => ProductErrors.SkuAlreadyExists,
            "price" => ProductErrors.InvalidPrice,
            "quantityIncrement" => ProductErrors.InvalidQuantityIncrement,
            "sellingUnit" => ProductErrors.InvalidSellingUnit,
            "name" when ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) => ProductErrors.VariantNameRequired,
            "name" => ProductErrors.VariantNameTooLong,
            "sku" when ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) => ProductErrors.SkuRequired,
            "sku" => ProductErrors.SkuTooLong,
            _ => ProductErrors.InvalidQuantityIncrement
        };
}
