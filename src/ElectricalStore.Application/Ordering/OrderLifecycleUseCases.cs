using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Logging;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Ordering;

public sealed class ConfirmOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IInventoryRepository _inventory;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<ConfirmOrderUseCase> _logger;

    public ConfirmOrderUseCase(
        IOrderRepository orders,
        IInventoryRepository inventory,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<ConfirmOrderUseCase> logger)
    {
        _orders = orders;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        if (order.Status != OrderStatus.PendingConfirmation)
            return Result.Failure<OrderDto>(OrderingErrors.InvalidTransition);

        var variantIds = order.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var inventoryItems = await _inventory.GetTrackedByVariantIdsAsync(variantIds, cancellationToken);
        var byVariant = inventoryItems.ToDictionary(x => x.ProductVariantId);

        foreach (var variantId in variantIds)
        {
            if (!byVariant.ContainsKey(variantId))
            {
                var created = InventoryItem.CreateZero(variantId);
                await _inventory.AddAsync(created, cancellationToken);
                byVariant[variantId] = created;
            }
        }

        foreach (var line in order.Items)
        {
            var item = byVariant[line.ProductVariantId];
            if (line.Quantity > item.Available)
                return Result.Failure<OrderDto>(OrderingErrors.ConfirmationStockConflict);
        }

        try
        {
            foreach (var line in order.Items)
            {
                byVariant[line.ProductVariantId].Reserve(line.Quantity);
            }

            order.Confirm(_clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConfirmationStockConflict);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (InventoryConcurrencyConflictException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConfirmationStockConflict);
        }
        catch (OrderConcurrencyConflictException)
        {
            return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);
        }

        _logger.LogInformation(
            "Order confirmed. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }
}

public sealed class PrepareOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<PrepareOrderUseCase> _logger;

    public PrepareOrderUseCase(
        IOrderRepository orders,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<PrepareOrderUseCase> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        try
        {
            order.MarkPreparing(_clock.UtcNow);
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
            "Order preparing. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }
}

public sealed class OutForDeliveryOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IInventoryRepository _inventory;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<OutForDeliveryOrderUseCase> _logger;

    public OutForDeliveryOrderUseCase(
        IOrderRepository orders,
        IInventoryRepository inventory,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<OutForDeliveryOrderUseCase> logger)
    {
        _orders = orders;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        if (order.Status != OrderStatus.Preparing)
            return Result.Failure<OrderDto>(OrderingErrors.InvalidTransition);

        var variantIds = order.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var inventoryItems = await _inventory.GetTrackedByVariantIdsAsync(variantIds, cancellationToken);
        var byVariant = inventoryItems.ToDictionary(x => x.ProductVariantId);

        try
        {
            foreach (var line in order.Items)
            {
                if (!byVariant.TryGetValue(line.ProductVariantId, out var item))
                    return Result.Failure<OrderDto>(OrderingErrors.ConfirmationStockConflict);

                item.Dispatch(line.Quantity);
            }

            order.MarkOutForDelivery(_clock.UtcNow);
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
            "Order out for delivery. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }
}

public sealed class DeliverOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<DeliverOrderUseCase> _logger;

    public DeliverOrderUseCase(
        IOrderRepository orders,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<DeliverOrderUseCase> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        try
        {
            order.MarkDelivered(_clock.UtcNow);
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
            "Order delivered. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }
}

public sealed class MarkOrderPaidUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly ILogger<MarkOrderPaidUseCase> _logger;

    public MarkOrderPaidUseCase(
        IOrderRepository orders,
        IAppUnitOfWork unitOfWork,
        ILogger<MarkOrderPaidUseCase> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetTrackedByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        try
        {
            order.MarkPaid();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already paid", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<OrderDto>(OrderingErrors.AlreadyPaid);
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
            "Order marked paid. OrderId={OrderId} OrderNumber={OrderNumber} PaymentStatus={PaymentStatus} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.PaymentStatus,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order));
    }
}
