using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Catalog.Products;

public static class ProductErrors
{
    public static Error NotFound { get; } =
        new("Product.NotFound", "Product was not found.");

    public static Error VariantNotFound { get; } =
        new("Product.VariantNotFound", "Product variant was not found.");

    public static Error CategoryNotFound { get; } =
        new("Product.CategoryNotFound", "Category was not found.");

    public static Error NameRequired { get; } =
        new("Product.NameRequired", "Product name is required.");

    public static Error NameTooLong { get; } =
        new("Product.NameTooLong", "Product name is too long.");

    public static Error DescriptionTooLong { get; } =
        new("Product.DescriptionTooLong", "Product description is too long.");

    public static Error CategoryRequired { get; } =
        new("Product.CategoryRequired", "Category is required.");

    public static Error VariantsRequired { get; } =
        new("Product.VariantsRequired", "At least one variant is required.");

    public static Error DuplicateVariantName { get; } =
        new("Product.DuplicateVariantName", "Variant names must be unique within the product.");

    public static Error DuplicateSkuInRequest { get; } =
        new("Product.DuplicateSkuInRequest", "Duplicate SKUs in the request are not allowed.");

    public static Error SkuAlreadyExists { get; } =
        new("Product.SkuAlreadyExists", "A variant with this SKU already exists.");

    public static Error VariantNameRequired { get; } =
        new("Product.VariantNameRequired", "Variant name is required.");

    public static Error VariantNameTooLong { get; } =
        new("Product.VariantNameTooLong", "Variant name is too long.");

    public static Error SkuRequired { get; } =
        new("Product.SkuRequired", "SKU is required.");

    public static Error SkuTooLong { get; } =
        new("Product.SkuTooLong", "SKU is too long.");

    public static Error InvalidPrice { get; } =
        new("Product.InvalidPrice", "Price must be greater than zero.");

    public static Error InvalidQuantityIncrement { get; } =
        new("Product.InvalidQuantityIncrement", "Quantity increment is invalid for the selling unit.");

    public static Error InvalidSellingUnit { get; } =
        new("Product.InvalidSellingUnit", "Selling unit is invalid.");

    public static Error TooManyImages { get; } =
        new("Product.TooManyImages", "A product may have at most 4 images.");

    public static Error ImageNotFound { get; } =
        new("Product.ImageNotFound", "Product image was not found.");

    public static Error InvalidImageReorder { get; } =
        new("Product.InvalidImageReorder", "Image reorder must include each existing image exactly once.");
}
