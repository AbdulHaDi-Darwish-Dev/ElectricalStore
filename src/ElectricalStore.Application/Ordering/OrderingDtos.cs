using ElectricalStore.Domain.Ordering;

namespace ElectricalStore.Application.Ordering;

public sealed record CheckoutLineRequest(Guid VariantId, decimal Quantity);

public sealed record CheckoutPreviewRequest(
    IReadOnlyList<CheckoutLineRequest> Items,
    Guid DeliveryZoneId);

public sealed record PlaceOrderRequest(
    IReadOnlyList<CheckoutLineRequest> Items,
    Guid DeliveryZoneId,
    string CustomerName,
    string Phone,
    string AddressText,
    string? CustomerNote);

public sealed record ModifyPendingOrderRequest(
    IReadOnlyList<CheckoutLineRequest> Items,
    Guid DeliveryZoneId,
    string? Reason);

public sealed record CancelOrderRequest(string? Reason);

public sealed record CheckoutLineDto(
    Guid ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    Guid VariantId,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal QuantityIncrement,
    decimal Quantity,
    decimal UnitPrice,
    decimal AvailableQuantity,
    decimal LineTotal);

public sealed record CheckoutPreviewDto(
    IReadOnlyList<CheckoutLineDto> Items,
    Guid DeliveryZoneId,
    string DeliveryZoneName,
    decimal ShippingFee,
    decimal MerchandiseSubtotal,
    decimal AppliedMinimumOrderAmount,
    decimal Total,
    bool MeetsMinimumOrder);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderModificationAuditDto(
    Guid Id,
    Guid PerformedByUserId,
    string Reason,
    string Summary,
    DateTime CreatedAtUtc);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    string CustomerName,
    string Phone,
    string AddressText,
    string? CustomerNote,
    Guid DeliveryZoneId,
    string DeliveryZoneName,
    decimal ShippingFee,
    decimal MerchandiseSubtotal,
    decimal AppliedMinimumOrderAmount,
    decimal Total,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? PreparingAtUtc,
    DateTime? OutForDeliveryAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    IReadOnlyList<OrderItemDto> Items,
    string? GuestAccessToken = null,
    string? TrackingHint = null,
    IReadOnlyList<OrderModificationAuditDto>? ModificationAudits = null)
{
    public static OrderDto From(
        Order order,
        string? guestAccessToken = null,
        bool includeModificationAudits = false) =>
        new(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentMethod.ToString(),
            order.PaymentStatus.ToString(),
            order.CustomerName,
            order.Phone,
            order.AddressText,
            order.CustomerNote,
            order.DeliveryZoneId,
            order.DeliveryZoneName,
            order.ShippingFee,
            order.MerchandiseSubtotal,
            order.AppliedMinimumOrderAmount,
            order.Total,
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.PreparingAtUtc,
            order.OutForDeliveryAtUtc,
            order.DeliveredAtUtc,
            order.CancelledAtUtc,
            order.CancellationReason,
            order.Items.OrderBy(i => i.Id).Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductVariantId,
                i.ProductName,
                i.VariantName,
                i.Sku,
                i.SellingUnit,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal)).ToList(),
            guestAccessToken,
            guestAccessToken is null ? null : $"/orders/{order.Id}/track",
            includeModificationAudits
                ? order.ModificationAudits
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Select(a => new OrderModificationAuditDto(
                        a.Id,
                        a.PerformedByUserId,
                        a.Reason,
                        a.Summary,
                        a.CreatedAtUtc))
                    .ToList()
                : null);
}

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string CustomerName,
    string Phone,
    decimal Total,
    DateTime CreatedAtUtc);

