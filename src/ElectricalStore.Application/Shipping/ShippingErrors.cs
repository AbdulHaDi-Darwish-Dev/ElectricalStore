using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Shipping;

namespace ElectricalStore.Application.Shipping;

public static class ShippingErrors
{
    public static Error NotFound { get; } =
        new("Shipping.NotFound", "Delivery zone was not found.");

    public static Error NameRequired { get; } =
        new("Shipping.NameRequired", "Delivery zone name is required.");

    public static Error NameTooLong { get; } =
        new("Shipping.NameTooLong", $"Delivery zone name must be {DeliveryZone.NameMaxLength} characters or fewer.");

    public static Error NegativeFee { get; } =
        new("Shipping.NegativeFee", "Delivery zone fee cannot be negative.");

    public static Error NameAlreadyExists { get; } =
        new("Shipping.NameAlreadyExists", "A delivery zone with this name already exists.");
}
