using ElectricalStore.Domain.Shipping;

namespace ElectricalStore.Application.Abstractions;

public interface IDeliveryZoneRepository
{
    Task AddAsync(DeliveryZone zone, CancellationToken cancellationToken = default);

    Task<DeliveryZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeliveryZone?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryZone>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
