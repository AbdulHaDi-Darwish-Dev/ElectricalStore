using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Logging;
using ElectricalStore.Domain.Inventory;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Inventory;

public sealed class AdjustInventoryUseCase
{
    private readonly IInventoryRepository _inventory;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<AdjustInventoryUseCase> _logger;

    public AdjustInventoryUseCase(
        IInventoryRepository inventory,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<AdjustInventoryUseCase> logger)
    {
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<InventoryItemDto>> ExecuteAsync(
        Guid variantId,
        AdjustInventoryRequest request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (performedByUserId == Guid.Empty)
            return Result.Failure<InventoryItemDto>(InventoryErrors.ActorRequired);

        if (request.QuantityDelta == 0m)
            return Result.Failure<InventoryItemDto>(InventoryErrors.ZeroAdjustment);

        string reason;
        try
        {
            reason = InventoryAdjustment.NormalizeReason(request.Reason);
        }
        catch (ArgumentException)
        {
            return Result.Failure<InventoryItemDto>(
                string.IsNullOrWhiteSpace(request.Reason)
                    ? InventoryErrors.ReasonRequired
                    : InventoryErrors.ReasonTooLong);
        }

        if (!await _inventory.VariantExistsAsync(variantId, cancellationToken))
            return Result.Failure<InventoryItemDto>(InventoryErrors.VariantNotFound);

        var item = await _inventory.GetTrackedByVariantIdAsync(variantId, cancellationToken);
        if (item is null)
        {
            item = InventoryItem.CreateZero(variantId);
            await _inventory.AddAsync(item, cancellationToken);
        }

        var before = item.OnHand;
        try
        {
            item.AdjustOnHand(request.QuantityDelta);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("cannot be negative", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<InventoryItemDto>(InventoryErrors.OnHandWouldBeNegative);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("cannot exceed on-hand", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<InventoryItemDto>(InventoryErrors.OnHandBelowReserved);
        }

        var adjustment = InventoryAdjustment.Create(
            variantId,
            request.QuantityDelta,
            before,
            item.OnHand,
            reason,
            performedByUserId,
            _clock.UtcNow);
        await _inventory.AddAdjustmentAsync(adjustment, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (InventoryConcurrencyConflictException)
        {
            return Result.Failure<InventoryItemDto>(InventoryErrors.ConcurrencyConflict);
        }

        _logger.LogInformation(
            "Inventory adjusted. AdjustmentId={AdjustmentId} VariantId={VariantId} QuantityDelta={QuantityDelta} OnHand={OnHand} Reserved={Reserved} Available={Available} ActorUserId={ActorUserId} TraceId={TraceId}",
            adjustment.Id,
            variantId,
            request.QuantityDelta,
            item.OnHand,
            item.Reserved,
            item.Available,
            performedByUserId,
            LogCorrelation.TraceId);

        var row = await _inventory.GetAdminRowByVariantIdAsync(variantId, cancellationToken);
        if (row is null)
            return Result.Failure<InventoryItemDto>(InventoryErrors.VariantNotFound);

        return Result.Success(InventoryItemDto.From(row));
    }
}
