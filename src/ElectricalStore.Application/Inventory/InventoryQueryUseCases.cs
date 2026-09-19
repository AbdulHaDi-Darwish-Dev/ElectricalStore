using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Inventory;

public sealed class ListAdminInventoryUseCase
{
    private readonly IInventoryRepository _inventory;

    public ListAdminInventoryUseCase(IInventoryRepository inventory)
    {
        _inventory = inventory;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> ExecuteAsync(
        InventoryListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await _inventory.ListAdminAsync(filter, cancellationToken);
        return rows.Select(InventoryItemDto.From).ToList();
    }
}

public sealed class GetAdminInventoryByVariantIdUseCase
{
    private readonly IInventoryRepository _inventory;

    public GetAdminInventoryByVariantIdUseCase(IInventoryRepository inventory)
    {
        _inventory = inventory;
    }

    public async Task<Result<InventoryItemDto>> ExecuteAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var row = await _inventory.GetAdminRowByVariantIdAsync(variantId, cancellationToken);
        if (row is null)
            return Result.Failure<InventoryItemDto>(InventoryErrors.VariantNotFound);

        return Result.Success(InventoryItemDto.From(row));
    }
}

public sealed class ListInventoryAdjustmentsUseCase
{
    private readonly IInventoryRepository _inventory;

    public ListInventoryAdjustmentsUseCase(IInventoryRepository inventory)
    {
        _inventory = inventory;
    }

    public async Task<Result<IReadOnlyList<InventoryAdjustmentDto>>> ExecuteAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        if (!await _inventory.VariantExistsAsync(variantId, cancellationToken))
            return Result.Failure<IReadOnlyList<InventoryAdjustmentDto>>(InventoryErrors.VariantNotFound);

        var adjustments = await _inventory.ListAdjustmentsAsync(variantId, cancellationToken);
        return Result.Success<IReadOnlyList<InventoryAdjustmentDto>>(
            adjustments.Select(InventoryAdjustmentDto.From).ToList());
    }
}
