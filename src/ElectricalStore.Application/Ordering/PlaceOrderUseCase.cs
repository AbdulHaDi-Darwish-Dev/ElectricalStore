using System.Security.Cryptography;
using System.Text;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Logging;
using ElectricalStore.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Ordering;

public sealed class PlaceOrderUseCase
{
    private readonly CheckoutPricingService _pricing;
    private readonly IOrderRepository _orders;
    private readonly IOrderPlacementIdempotencyRepository _idempotency;
    private readonly IGuestOrderTokenService _tokens;
    private readonly IGuestAccessTokenProtector _guestTokenProtector;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;
    private readonly ILogger<PlaceOrderUseCase> _logger;

    public PlaceOrderUseCase(
        CheckoutPricingService pricing,
        IOrderRepository orders,
        IOrderPlacementIdempotencyRepository idempotency,
        IGuestOrderTokenService tokens,
        IGuestAccessTokenProtector guestTokenProtector,
        IAppUnitOfWork unitOfWork,
        IAppClock clock,
        ILogger<PlaceOrderUseCase> logger)
    {
        _pricing = pricing;
        _orders = orders;
        _idempotency = idempotency;
        _tokens = tokens;
        _guestTokenProtector = guestTokenProtector;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> ExecuteAsync(
        PlaceOrderRequest request,
        Guid? authenticatedUserId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var keyValidation = ValidateIdempotencyKey(idempotencyKey);
        if (keyValidation.IsFailure)
            return Result.Failure<OrderDto>(keyValidation.Error!);

        var rawKey = idempotencyKey!.Trim();
        var keyHash = HashIdempotencyKey(rawKey);
        var scope = authenticatedUserId is Guid uid && uid != Guid.Empty
            ? OrderPlacementIdempotency.ScopeForUser(uid)
            : OrderPlacementIdempotency.ScopeForGuest();

        var existing = await _idempotency.FindAsync(scope, keyHash, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsExpired(_clock.UtcNow))
                return await ReplayAsync(existing, cancellationToken);

            await _idempotency.RemoveAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return Result.Failure<OrderDto>(OrderingErrors.CustomerNameRequired);
        if (string.IsNullOrWhiteSpace(request.Phone))
            return Result.Failure<OrderDto>(OrderingErrors.PhoneRequired);
        if (string.IsNullOrWhiteSpace(request.AddressText))
            return Result.Failure<OrderDto>(OrderingErrors.AddressRequired);

        var resolved = await _pricing.ResolveAsync(
            request.Items,
            request.DeliveryZoneId,
            minimumOverride: null,
            cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<OrderDto>(resolved.Error!);

        var r = resolved.Value;
        if (!r.MeetsMinimumOrder)
            return Result.Failure<OrderDto>(OrderingErrors.BelowMinimumOrder);

        string? rawGuestToken = null;
        string? guestHash = null;
        string? protectedGuestToken = null;
        Guid? userId = null;

        if (authenticatedUserId is Guid authId && authId != Guid.Empty)
        {
            userId = authId;
        }
        else
        {
            (rawGuestToken, guestHash) = _tokens.CreateToken();
            protectedGuestToken = _guestTokenProtector.Protect(rawGuestToken);
        }

        var orderNumber = await CreateUniqueOrderNumberAsync(cancellationToken);
        var seeds = r.Lines.Select(l => new OrderItemSeed(
            l.ProductId,
            l.VariantId,
            l.ProductName,
            l.VariantName,
            l.Sku,
            l.SellingUnit,
            l.Quantity,
            l.UnitPrice)).ToList();

        Order order;
        try
        {
            order = Order.Place(
                orderNumber,
                userId,
                guestHash,
                request.CustomerName,
                request.Phone,
                request.AddressText,
                request.CustomerNote,
                r.DeliveryZoneId,
                r.DeliveryZoneName,
                r.ShippingFee,
                r.AppliedMinimumOrderAmount,
                seeds,
                _clock.UtcNow);
        }
        catch (ArgumentException ex) when (ex.ParamName == "customerName")
        {
            return Result.Failure<OrderDto>(OrderingErrors.CustomerNameRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "phone")
        {
            return Result.Failure<OrderDto>(OrderingErrors.PhoneRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "addressText")
        {
            return Result.Failure<OrderDto>(OrderingErrors.AddressRequired);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("minimum", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<OrderDto>(OrderingErrors.BelowMinimumOrder);
        }

        var idempotency = OrderPlacementIdempotency.Create(
            keyHash,
            scope,
            order.Id,
            protectedGuestToken,
            _clock.UtcNow);

        await _orders.AddAsync(order, cancellationToken);
        await _idempotency.AddAsync(idempotency, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            var winner = await _idempotency.FindAsync(scope, keyHash, cancellationToken);
            if (winner is null || winner.IsExpired(_clock.UtcNow))
                return Result.Failure<OrderDto>(OrderingErrors.ConcurrencyConflict);

            return await ReplayAsync(winner, cancellationToken);
        }

        _logger.LogInformation(
            "Order placed. OrderId={OrderId} OrderNumber={OrderNumber} Status={Status} UserId={UserId} DeliveryZoneId={DeliveryZoneId} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            order.Status,
            order.UserId,
            order.DeliveryZoneId,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order, rawGuestToken));
    }

    private async Task<Result<OrderDto>> ReplayAsync(
        OrderPlacementIdempotency record,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(record.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        string? rawGuestToken = null;
        if (!string.IsNullOrWhiteSpace(record.ProtectedGuestAccessToken))
        {
            rawGuestToken = _guestTokenProtector.Unprotect(record.ProtectedGuestAccessToken);
            if (rawGuestToken is null)
                return Result.Failure<OrderDto>(OrderingErrors.IdempotencyReplayUnavailable);
        }

        _logger.LogDebug(
            "Place Order idempotency replay. OrderId={OrderId} OrderNumber={OrderNumber} TraceId={TraceId}",
            order.Id,
            order.OrderNumber,
            LogCorrelation.TraceId);

        return Result.Success(OrderDto.From(order, rawGuestToken));
    }

    private static Result ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result.Failure(OrderingErrors.IdempotencyKeyRequired);

        var trimmed = idempotencyKey.Trim();
        if (trimmed.Length < OrderPlacementIdempotency.RawKeyMinLength
            || trimmed.Length > OrderPlacementIdempotency.RawKeyMaxLength)
        {
            return Result.Failure(OrderingErrors.InvalidIdempotencyKey);
        }

        return Result.Success();
    }

    private static string HashIdempotencyKey(string rawKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hash);
    }

    private async Task<string> CreateUniqueOrderNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var candidate = $"ES-{_clock.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
            if (!await _orders.OrderNumberExistsAsync(candidate, cancellationToken))
                return candidate;
        }

        return $"ES-{_clock.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";
    }
}
