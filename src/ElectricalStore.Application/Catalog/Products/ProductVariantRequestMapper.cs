using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Catalog.Products;

namespace ElectricalStore.Application.Catalog.Products;

internal static class ProductVariantRequestMapper
{
    public static Result<ProductVariantSeed> ToSeed(CreateProductVariantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseSellingUnit(request.SellingUnit, out var unit))
            return Result.Failure<ProductVariantSeed>(ProductErrors.InvalidSellingUnit);

        try
        {
            // Force domain validation path via temporary create on throw-only helpers.
            _ = ProductVariant.NormalizeNameKey(request.Name);
            _ = ProductVariant.NormalizeSkuKey(request.Sku);
            if (request.Price <= 0m)
                return Result.Failure<ProductVariantSeed>(ProductErrors.InvalidPrice);
            if (request.QuantityIncrement <= 0m)
                return Result.Failure<ProductVariantSeed>(ProductErrors.InvalidQuantityIncrement);
            if (unit == SellingUnit.Piece && request.QuantityIncrement != 1m)
                return Result.Failure<ProductVariantSeed>(ProductErrors.InvalidQuantityIncrement);

            return Result.Success(new ProductVariantSeed(
                request.Name,
                request.Sku,
                request.Price,
                unit,
                request.QuantityIncrement,
                request.IsActive));
        }
        catch (ArgumentException ex) when (ex.ParamName == "name" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ProductVariantSeed>(ProductErrors.VariantNameRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name")
        {
            return Result.Failure<ProductVariantSeed>(ProductErrors.VariantNameTooLong);
        }
        catch (ArgumentException ex) when (ex.ParamName == "sku" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ProductVariantSeed>(ProductErrors.SkuRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "sku")
        {
            return Result.Failure<ProductVariantSeed>(ProductErrors.SkuTooLong);
        }
    }

    public static Result<ProductVariantSeed> ToSeed(UpdateProductVariantRequest request, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ToSeed(new CreateProductVariantRequest(
            request.Name,
            request.Sku,
            request.Price,
            request.SellingUnit,
            request.QuantityIncrement,
            isActive));
    }

    public static bool TryParseSellingUnit(string? value, out SellingUnit unit)
    {
        unit = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Enum.TryParse(value.Trim(), ignoreCase: true, out unit)
               && Enum.IsDefined(unit);
    }
}
