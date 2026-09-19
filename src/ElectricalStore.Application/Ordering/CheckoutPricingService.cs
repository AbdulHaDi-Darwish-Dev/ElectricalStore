using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Ordering;

namespace ElectricalStore.Application.Ordering;

/// <summary>
/// Shared authoritative checkout validation for Preview and Place Order.
/// Never trusts client prices/totals/stock/shipping fees.
/// Minimum order comes from persisted OrderingSettings unless overridden (Pending modify).
/// </summary>
public sealed class CheckoutPricingService
{
    private readonly IOrderCatalogQuery _catalog;
    private readonly IInventoryRepository _inventory;
    private readonly IDeliveryZoneRepository _zones;
    private readonly IOrderingSettingsRepository _settings;

    public CheckoutPricingService(
        IOrderCatalogQuery catalog,
        IInventoryRepository inventory,
        IDeliveryZoneRepository zones,
        IOrderingSettingsRepository settings)
    {
        _catalog = catalog;
        _inventory = inventory;
        _zones = zones;
        _settings = settings;
    }

    public async Task<Result<ResolvedCheckout>> ResolveAsync(
        IReadOnlyList<CheckoutLineRequest> items,
        Guid deliveryZoneId,
        decimal? minimumOverride,
        CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
            return Result.Failure<ResolvedCheckout>(OrderingErrors.EmptyItems);

        var normalized = new List<(Guid VariantId, decimal Quantity)>();
        var seen = new HashSet<Guid>();
        foreach (var line in items)
        {
            if (line.Quantity <= 0m)
                return Result.Failure<ResolvedCheckout>(OrderingErrors.QuantityMustBePositive);
            if (!seen.Add(line.VariantId))
                return Result.Failure<ResolvedCheckout>(OrderingErrors.DuplicateVariant);
            normalized.Add((line.VariantId, line.Quantity));
        }

        var variantIds = normalized.Select(x => x.VariantId).ToList();
        var catalog = await _catalog.GetCatalogLinesAsync(variantIds, cancellationToken);
        var availability = await _inventory.GetAvailabilityMapAsync(variantIds, cancellationToken);

        var resolvedLines = new List<ResolvedCheckoutLine>();
        foreach (var (variantId, quantity) in normalized)
        {
            if (!catalog.TryGetValue(variantId, out var info))
                return Result.Failure<ResolvedCheckout>(OrderingErrors.VariantNotFound);

            if (!info.IsCatalogReady)
                return Result.Failure<ResolvedCheckout>(OrderingErrors.NotPurchasable);

            if (!QuantityRules.IsValidQuantity(quantity, info.QuantityIncrement))
                return Result.Failure<ResolvedCheckout>(OrderingErrors.InvalidQuantityIncrement);

            var available = availability.TryGetValue(variantId, out var stock) ? stock.Available : 0m;
            if (quantity > available)
                return Result.Failure<ResolvedCheckout>(OrderingErrors.InsufficientStock);

            var unitPrice = Money.Round(info.UnitPrice);
            var lineTotal = Money.Round(unitPrice * quantity);
            resolvedLines.Add(new ResolvedCheckoutLine(
                info.ProductId,
                info.ProductName,
                info.PrimaryImageUrl,
                info.ProductVariantId,
                info.VariantName,
                info.Sku,
                info.SellingUnit,
                info.QuantityIncrement,
                quantity,
                unitPrice,
                available,
                lineTotal));
        }

        var zone = await _zones.GetByIdAsync(deliveryZoneId, cancellationToken);
        if (zone is null)
            return Result.Failure<ResolvedCheckout>(OrderingErrors.DeliveryZoneNotFound);
        if (!zone.IsActive)
            return Result.Failure<ResolvedCheckout>(OrderingErrors.DeliveryZoneInactive);

        var merchandise = Money.Round(resolvedLines.Sum(l => l.LineTotal));
        var shipping = Money.Round(zone.Fee);

        decimal minimum;
        if (minimumOverride is decimal overrideMin)
        {
            minimum = Money.Round(overrideMin);
        }
        else
        {
            var settings = await _settings.GetOrCreateAsync(cancellationToken);
            minimum = Money.Round(settings.MinimumMerchandiseSubtotal);
        }

        if (minimum < 0m)
            minimum = 0m;

        var meetsMinimum = merchandise >= minimum;
        var total = Money.Round(merchandise + shipping);

        return Result.Success(new ResolvedCheckout(
            resolvedLines,
            zone.Id,
            zone.Name,
            shipping,
            merchandise,
            minimum,
            total,
            meetsMinimum));
    }
}

public sealed record ResolvedCheckoutLine(
    Guid ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    Guid VariantId,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal QuantityIncrement,
    decimal Quantity,
    decimal UnitPrice,
    decimal AvailableQuantity,
    decimal LineTotal);

public sealed record ResolvedCheckout(
    IReadOnlyList<ResolvedCheckoutLine> Lines,
    Guid DeliveryZoneId,
    string DeliveryZoneName,
    decimal ShippingFee,
    decimal MerchandiseSubtotal,
    decimal AppliedMinimumOrderAmount,
    decimal Total,
    bool MeetsMinimumOrder);
