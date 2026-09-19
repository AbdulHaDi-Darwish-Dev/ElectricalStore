using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Ordering;

public sealed class CheckoutPreviewUseCase
{
    private readonly CheckoutPricingService _pricing;

    public CheckoutPreviewUseCase(CheckoutPricingService pricing)
    {
        _pricing = pricing;
    }

    public async Task<Result<CheckoutPreviewDto>> ExecuteAsync(
        CheckoutPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolved = await _pricing.ResolveAsync(
            request.Items,
            request.DeliveryZoneId,
            minimumOverride: null,
            cancellationToken);

        if (resolved.IsFailure)
            return Result.Failure<CheckoutPreviewDto>(resolved.Error!);

        var r = resolved.Value;
        return Result.Success(new CheckoutPreviewDto(
            r.Lines.Select(l => new CheckoutLineDto(
                l.ProductId,
                l.ProductName,
                l.PrimaryImageUrl,
                l.VariantId,
                l.VariantName,
                l.Sku,
                l.SellingUnit,
                l.QuantityIncrement,
                l.Quantity,
                l.UnitPrice,
                l.AvailableQuantity,
                l.LineTotal)).ToList(),
            r.DeliveryZoneId,
            r.DeliveryZoneName,
            r.ShippingFee,
            r.MerchandiseSubtotal,
            r.AppliedMinimumOrderAmount,
            r.Total,
            r.MeetsMinimumOrder));
    }
}
