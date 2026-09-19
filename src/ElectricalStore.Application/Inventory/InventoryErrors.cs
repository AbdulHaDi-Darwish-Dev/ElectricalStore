using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Inventory;

namespace ElectricalStore.Application.Inventory;

public static class InventoryErrors
{
    public static Error VariantNotFound { get; } =
        new("Inventory.VariantNotFound", "Product variant was not found.");

    public static Error ZeroAdjustment { get; } =
        new("Inventory.ZeroAdjustment", "Quantity delta cannot be zero.");

    public static Error ReasonRequired { get; } =
        new("Inventory.ReasonRequired", "Adjustment reason is required.");

    public static Error ReasonTooLong { get; } =
        new("Inventory.ReasonTooLong", $"Adjustment reason must be {InventoryAdjustment.ReasonMaxLength} characters or fewer.");

    public static Error InvalidQuantity { get; } =
        new("Inventory.InvalidQuantity", "Quantity is invalid.");

    public static Error OnHandWouldBeNegative { get; } =
        new("Inventory.OnHandWouldBeNegative", "Adjustment would make on-hand quantity negative.");

    public static Error OnHandBelowReserved { get; } =
        new("Inventory.OnHandBelowReserved", "Adjustment would make on-hand quantity below reserved quantity.");

    public static Error ConcurrencyConflict { get; } =
        new("Inventory.ConcurrencyConflict", "Inventory was modified by another request. Retry the adjustment.");

    public static Error InsufficientAvailable { get; } =
        new("Inventory.InsufficientAvailable", "Insufficient available quantity.");

    public static Error InsufficientReserved { get; } =
        new("Inventory.InsufficientReserved", "Insufficient reserved quantity.");

    public static Error ActorRequired { get; } =
        new("Inventory.ActorRequired", "Authenticated user is required to adjust inventory.");
}
