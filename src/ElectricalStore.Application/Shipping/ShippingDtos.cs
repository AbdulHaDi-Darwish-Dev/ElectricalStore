using ElectricalStore.Domain.Shipping;

namespace ElectricalStore.Application.Shipping;

public sealed record DeliveryZoneDto(
    Guid Id,
    string Name,
    decimal Fee,
    bool IsActive)
{
    public static DeliveryZoneDto From(DeliveryZone zone) =>
        new(zone.Id, zone.Name, zone.Fee, zone.IsActive);
}

/// <summary>Public / checkout-facing active zone (no IsActive flag — inactive are omitted).</summary>
public sealed record PublicDeliveryZoneDto(Guid Id, string Name, decimal Fee)
{
    public static PublicDeliveryZoneDto From(DeliveryZone zone) =>
        new(zone.Id, zone.Name, zone.Fee);
}

public sealed record CreateDeliveryZoneRequest(string Name, decimal Fee, bool IsActive);

public sealed record UpdateDeliveryZoneRequest(string Name, decimal Fee);
