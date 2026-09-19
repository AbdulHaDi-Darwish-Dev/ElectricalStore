using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Shipping;

public sealed class GetAdminDeliveryZoneByIdUseCase
{
    private readonly IDeliveryZoneRepository _repository;

    public GetAdminDeliveryZoneByIdUseCase(IDeliveryZoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DeliveryZoneDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var zone = await _repository.GetByIdAsync(id, cancellationToken);
        if (zone is null)
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NotFound);

        return Result.Success(DeliveryZoneDto.From(zone));
    }
}

public sealed class ListAdminDeliveryZonesUseCase
{
    private readonly IDeliveryZoneRepository _repository;

    public ListAdminDeliveryZonesUseCase(IDeliveryZoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<DeliveryZoneDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var zones = await _repository.ListAsync(activeOnly: false, cancellationToken);
        return zones.Select(DeliveryZoneDto.From).ToList();
    }
}

public sealed class ListActiveDeliveryZonesUseCase
{
    private readonly IDeliveryZoneRepository _repository;

    public ListActiveDeliveryZonesUseCase(IDeliveryZoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PublicDeliveryZoneDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var zones = await _repository.ListAsync(activeOnly: true, cancellationToken);
        return zones.Select(PublicDeliveryZoneDto.From).ToList();
    }
}
