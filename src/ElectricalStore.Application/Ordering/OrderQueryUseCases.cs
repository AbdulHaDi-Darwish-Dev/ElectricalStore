using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Ordering;

namespace ElectricalStore.Application.Ordering;

public sealed class GetOrderByIdUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IGuestOrderTokenService _tokens;

    public GetOrderByIdUseCase(IOrderRepository orders, IGuestOrderTokenService tokens)
    {
        _orders = orders;
        _tokens = tokens;
    }

    public async Task<Result<OrderDto>> ExecuteForCustomerAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.UserId != userId)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        return Result.Success(OrderDto.From(order));
    }

    public async Task<Result<OrderDto>> ExecuteForGuestAsync(
        Guid orderId,
        string guestRawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guestRawToken))
            return Result.Failure<OrderDto>(OrderingErrors.InvalidGuestToken);

        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null || string.IsNullOrWhiteSpace(order.GuestAccessTokenHash))
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        if (!_tokens.TokensMatch(guestRawToken, order.GuestAccessTokenHash))
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        return Result.Success(OrderDto.From(order));
    }

    public async Task<Result<OrderDto>> ExecuteForAdminAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderDto>(OrderingErrors.NotFound);

        return Result.Success(OrderDto.From(order, includeModificationAudits: true));
    }
}

public sealed class ListCustomerOrdersUseCase
{
    private readonly IOrderRepository _orders;

    public ListCustomerOrdersUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<Result<IReadOnlyList<OrderListItemDto>>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result.Failure<IReadOnlyList<OrderListItemDto>>(OrderingErrors.ActorRequired);

        var orders = await _orders.ListByUserIdAsync(userId, cancellationToken);
        return Result.Success<IReadOnlyList<OrderListItemDto>>(orders.Select(o => new OrderListItemDto(
            o.Id,
            o.OrderNumber,
            o.Status.ToString(),
            o.PaymentStatus.ToString(),
            o.CustomerName,
            o.Phone,
            o.Total,
            o.CreatedAtUtc)).ToList());
    }
}

public sealed class ListAdminOrdersUseCase
{
    private readonly IOrderRepository _orders;

    public ListAdminOrdersUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyList<OrderListItemDto>> ExecuteAsync(
        OrderAdminListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orders.ListAdminAsync(filter, cancellationToken);
        return orders.Select(o => new OrderListItemDto(
            o.Id,
            o.OrderNumber,
            o.Status.ToString(),
            o.PaymentStatus.ToString(),
            o.CustomerName,
            o.Phone,
            o.Total,
            o.CreatedAtUtc)).ToList();
    }
}
