using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Logging;
using ElectricalStore.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Ordering;

public sealed class CancelOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IInventoryRepository _inventory;
    private readonly IGuestOrderTokenService _tokens;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<CancelOrderUseCase> _logger;

    public CancelOrderUseCase(
        IOrderRepository orders,
        IInventoryRepository inventory,
        IGuestOrderTokenService tokens,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<CancelOrderUseCase> logger)
    {
        _orders = orders;
        _inventory = inventory;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Customer cancel: PendingConfirmation only.</summary>
    public async Task<Result<OrderDto>> ExecuteAsCustomerAsync(
        Guid orderId,
        Guid? userId,
        string? guestRawToken,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var access = await LoadAuthorizedTrackedAsync(orderId, userId, guestRawToken, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<OrderDto>(access.Error!);

        var order = access.Value;
        if (order.Status != OrderStatus.PendingConfirmation)
            return Result.Failure<OrderDto>(OrderingErrors.InvalidTransition);

        try
        {
            order.Cancel(userId, reason, _clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.InvalidTransition);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OrderConcurrencyConflictException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
        }

        _logger.LogInformation(
            "Order cancelled by customer. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} UserId={UserId} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            userId,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }

    /// <summary>Admin cancel: Pending/Confirmed/Preparing. Releases reservation when applicable.</summary>
    public async Task<Result<OrderDto>> ExecuteAsAdminAsync(
        Guid orderId,
        Guid adminUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            return Result.Failure<OrderDto>(OrderingErrors.ActorRequired);
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<OrderDto>(OrderingErrors.CancellationReasonRequired);

        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        if (order.Status is OrderStatus.OutForDelivery or OrderStatus.Delivered or OrderStatus.Cancelled)
            return Result.Failure<OrderDto>(OrderingErrors.InvalidTransition);

        var release = order.Status is OrderStatus.Confirmed or OrderStatus.Preparing;

        if (release)
        {
            var variantIds = order.Items.Select(i => i.ProductVariantId).Distinct().ToList();
            var inventoryItems = await _inventory.GetTrackedByVariantIdsAsync(variantIds, cancellationToken);
            var byVariant = inventoryItems.ToDictionary(x => x.ProductVariantId);

            try
            {
                foreach (var line in order.Items)
                {
                    if (!byVariant.TryGetValue(line.ProductVariantId, out var item))
                        return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
                    item.Release(line.Quantity);
                }
            }
            catch (InvalidOperationException)
            {
                return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
            }
        }

        try
        {
            order.Cancel(adminUserId, reason, _clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.InvalidTransition);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (InventoryConcurrencyConflictException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
        }
        catch (OrderConcurrencyConflictException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
        }

        _logger.LogInformation(
            "Order cancelled by admin. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} ActorUserId={ActorUserId} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            adminUserId,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }

    private async Task<Result<Order>> LoadAuthorizedTrackedAsync(
        Guid orderId,
        Guid? userId,
        string? guestRawToken,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<Order>(OrderingErrors.NotFound);

        if (userId is Guid uid && uid != Guid.Empty)
        {
            if (order.UserId != uid)
                return Result.Failure<Order>(OrderingErrors.NotFound);
            return Result.Success(order);
        }

        if (string.IsNullOrWhiteSpace(guestRawToken) || string.IsNullOrWhiteSpace(order.GuestAccessTokenHash))
            return Result.Failure<Order>(OrderingErrors.NotFound);

        if (!_tokens.TokensMatch(guestRawToken, order.GuestAccessTokenHash))
            return Result.Failure<Order>(OrderingErrors.NotFound);

        return Result.Success(order);
    }
}

public sealed class ModifyPendingOrderUseCase
{
    private readonly CheckoutPricingService _pricing;
    private readonly IOrderRepository _orders;
    private readonly IGuestOrderTokenService _tokens;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<ModifyPendingOrderUseCase> _logger;

    public ModifyPendingOrderUseCase(
        CheckoutPricingService pricing,
        IOrderRepository orders,
        IGuestOrderTokenService tokens,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<ModifyPendingOrderUseCase> logger)
    {
        _pricing = pricing;
        _orders = orders;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(
        Guid orderId,
        ModifyPendingOrderRequest request,
        Guid? customerUserId,
        string? guestRawToken,
        Guid? staffUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        if (order.Status != OrderStatus.PendingConfirmation)
            return Result.Failure<OrderDto>(OrderingErrors.CannotModify);

        if (staffUserId is null)
        {
            if (customerUserId is Guid uid && uid != Guid.Empty)
            {
                if (order.UserId != uid)
                    return Result.Failure<OrderDto>(OrderingErrors.NotFound);
            }
            else if (!string.IsNullOrWhiteSpace(guestRawToken) && !string.IsNullOrWhiteSpace(order.GuestAccessTokenHash))
            {
                if (!_tokens.TokensMatch(guestRawToken!, order.GuestAccessTokenHash!))
                    return Result.Failure<OrderDto>(OrderingErrors.NotFound);
            }
            else
            {
                return Result.Failure<OrderDto>(OrderingErrors.NotFound);
            }
        }

        var resolved = await _pricing.ResolveAsync(
            request.Items,
            request.DeliveryZoneId,
            minimumOverride: order.AppliedMinimumOrderAmount,
            cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<OrderDto>(resolved.Error!);

        var r = resolved.Value;
        if (!r.MeetsMinimumOrder)
            return Result.Failure<OrderDto>(OrderingErrors.BelowMinimumOrder);

        var seeds = r.Lines.Select(l => new OrderItemSeed(
            l.ProductId,
            l.VariantId,
            l.ProductName,
            l.VariantName,
            l.Sku,
            l.SellingUnit,
            l.Quantity,
            l.UnitPrice)).ToList();

        try
        {
            order.ReplacePendingItems(
                r.DeliveryZoneId,
                r.DeliveryZoneName,
                r.ShippingFee,
                seeds,
                staffUserId,
                request.Reason,
                _clock.UtcNow);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("minimum", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<OrderDto>(OrderingErrors.BelowMinimumOrder);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.CannotModify);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OrderConcurrencyConflictException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
        }

        _logger.LogInformation(
            "Pending order modified. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} ActorUserId={ActorUserId} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            staffUserId ?? customerUserId,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }
}
