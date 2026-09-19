using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Inventory;

namespace ElectricalStore.Application.Inventory;

public sealed record InventoryItemDto(
    Guid ProductId,
    string ProductName,
    Guid VariantId,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    bool IsInStock)
{
    public static InventoryItemDto From(InventoryListRow row) =>
        new(
            row.ProductId,
            row.ProductName,
            row.VariantId,
            row.VariantName,
            row.Sku,
            row.SellingUnit,
            row.OnHand,
            row.Reserved,
            row.OnHand - row.Reserved,
            row.OnHand - row.Reserved > 0m);
}

public sealed record AdjustInventoryRequest(decimal QuantityDelta, string Reason);

public sealed record InventoryAdjustmentDto(
    Guid Id,
    Guid ProductVariantId,
    decimal QuantityDelta,
    decimal OnHandBefore,
    decimal OnHandAfter,
    string Reason,
    Guid PerformedByUserId,
    DateTime CreatedAtUtc)
{
    public static InventoryAdjustmentDto From(InventoryAdjustment adjustment) =>
        new(
            adjustment.Id,
            adjustment.ProductVariantId,
            adjustment.QuantityDelta,
            adjustment.OnHandBefore,
            adjustment.OnHandAfter,
            adjustment.Reason,
            adjustment.PerformedByUserId,
            adjustment.CreatedAtUtc);
}
