namespace ElectricalStore.Domain.Ordering;

/// <summary>
/// Customer order aggregate. Place Order does not reserve stock; Admin Confirm does.
/// OutForDelivery dispatches reserved stock. Snapshots catalog/shipping at place/reprice.
/// </summary>
public sealed class Order
{
    public const int OrderNumberMaxLength = 32;
    public const int CustomerNameMaxLength = 200;
    public const int PhoneMaxLength = 32;
    public const int AddressMaxLength = 1000;
    public const int NoteMaxLength = 1000;
    public const int CancellationReasonMaxLength = 500;
    public const int GuestTokenHashMaxLength = 128;
    public const int ZoneNameMaxLength = 200;

    private readonly List<OrderItem> _items = new();
    private readonly List<OrderModificationAudit> _modificationAudits = new();

    public Guid Id { get; private set; }

    public string OrderNumber { get; private set; } = string.Empty;

    public Guid? UserId { get; private set; }

    public string? GuestAccessTokenHash { get; private set; }

    public OrderStatus Status { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string AddressText { get; private set; } = string.Empty;

    public string? CustomerNote { get; private set; }

    public Guid DeliveryZoneId { get; private set; }

    public string DeliveryZoneName { get; private set; } = string.Empty;

    public decimal ShippingFee { get; private set; }

    public decimal MerchandiseSubtotal { get; private set; }

    public decimal Total { get; private set; }

    /// <summary>Minimum merchandise subtotal applied at Place Order / reprice (historical).</summary>
    public decimal AppliedMinimumOrderAmount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? PreparingAtUtc { get; private set; }

    public DateTime? OutForDeliveryAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public string? CancellationReason { get; private set; }

    /// <summary>SQL Server rowversion for Order lifecycle races.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<OrderModificationAudit> ModificationAudits => _modificationAudits.AsReadOnly();

    private Order()
    {
    }

    public static Order Place(
        string orderNumber,
        Guid? userId,
        string? guestAccessTokenHash,
        string customerName,
        string phone,
        string addressText,
        string? customerNote,
        Guid deliveryZoneId,
        string deliveryZoneName,
        decimal shippingFee,
        decimal appliedMinimumOrderAmount,
        IReadOnlyList<OrderItemSeed> items,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));
        var number = orderNumber.Trim();
        if (number.Length > OrderNumberMaxLength)
            throw new ArgumentException($"Order number must be {OrderNumberMaxLength} characters or fewer.", nameof(orderNumber));

        if (userId is null && string.IsNullOrWhiteSpace(guestAccessTokenHash))
            throw new ArgumentException("Authenticated user or guest token hash is required.");
        if (userId is not null && !string.IsNullOrWhiteSpace(guestAccessTokenHash))
            throw new ArgumentException("Order cannot have both UserId and guest token.");

        if (items is null || items.Count == 0)
            throw new ArgumentException("Order requires at least one item.", nameof(items));

        EnsureUtc(utcNow, nameof(utcNow));
        if (deliveryZoneId == Guid.Empty)
            throw new ArgumentException("Delivery zone is required.", nameof(deliveryZoneId));
        if (shippingFee < 0m)
            throw new ArgumentOutOfRangeException(nameof(shippingFee));
        if (appliedMinimumOrderAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(appliedMinimumOrderAmount));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = number,
            UserId = userId,
            GuestAccessTokenHash = string.IsNullOrWhiteSpace(guestAccessTokenHash)
                ? null
                : guestAccessTokenHash.Trim(),
            Status = OrderStatus.PendingConfirmation,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Unpaid,
            CustomerName = RequireText(customerName, CustomerNameMaxLength, nameof(customerName)),
            Phone = RequireText(phone, PhoneMaxLength, nameof(phone)),
            AddressText = RequireText(addressText, AddressMaxLength, nameof(addressText)),
            CustomerNote = NormalizeOptional(customerNote, NoteMaxLength),
            DeliveryZoneId = deliveryZoneId,
            DeliveryZoneName = RequireText(deliveryZoneName, ZoneNameMaxLength, nameof(deliveryZoneName)),
            ShippingFee = Money.Round(shippingFee),
            AppliedMinimumOrderAmount = Money.Round(appliedMinimumOrderAmount),
            CreatedAtUtc = utcNow
        };

        foreach (var seed in items)
        {
            var lineTotal = Money.Round(seed.UnitPrice * seed.Quantity);
            order._items.Add(OrderItem.Create(
                order.Id,
                seed.ProductId,
                seed.ProductVariantId,
                seed.ProductName,
                seed.VariantName,
                seed.Sku,
                seed.SellingUnit,
                seed.Quantity,
                Money.Round(seed.UnitPrice),
                lineTotal));
        }

        order.RecalculateTotals();
        if (order.MerchandiseSubtotal < order.AppliedMinimumOrderAmount)
            throw new InvalidOperationException("Merchandise subtotal is below the applied minimum order amount.");

        return order;
    }

    public void Confirm(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (Status != OrderStatus.PendingConfirmation)
            throw new InvalidOperationException("Only PendingConfirmation orders can be confirmed.");

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = utcNow;
    }

    public void MarkPreparing(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only Confirmed orders can move to Preparing.");

        Status = OrderStatus.Preparing;
        PreparingAtUtc = utcNow;
    }

    public void MarkOutForDelivery(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (Status != OrderStatus.Preparing)
            throw new InvalidOperationException("Only Preparing orders can move to OutForDelivery.");

        Status = OrderStatus.OutForDelivery;
        OutForDeliveryAtUtc = utcNow;
    }

    public void MarkDelivered(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (Status != OrderStatus.OutForDelivery)
            throw new InvalidOperationException("Only OutForDelivery orders can be marked Delivered.");

        Status = OrderStatus.Delivered;
        DeliveredAtUtc = utcNow;
    }

    public void MarkPaid()
    {
        if (PaymentMethod != PaymentMethod.CashOnDelivery)
            throw new InvalidOperationException("Only COD orders can be marked paid in MVP.");
        if (PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Order is already paid.");
        if (Status is not (OrderStatus.OutForDelivery or OrderStatus.Delivered))
            throw new InvalidOperationException(
                "COD orders can only be marked paid when OutForDelivery or Delivered.");

        PaymentStatus = PaymentStatus.Paid;
    }

    /// <summary>
    /// Cancel Pending (no stock reserved) or Confirmed/Preparing (caller must Release reserved stock).
    /// Not allowed after OutForDelivery in MVP.
    /// </summary>
    public void Cancel(Guid? cancelledByUserId, string? reason, DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (Status is OrderStatus.OutForDelivery or OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new InvalidOperationException("Order cannot be cancelled in its current status.");

        if (Status is not (OrderStatus.PendingConfirmation or OrderStatus.Confirmed or OrderStatus.Preparing))
            throw new InvalidOperationException("Order cannot be cancelled in its current status.");

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is not null && trimmed.Length > CancellationReasonMaxLength)
            throw new ArgumentException(
                $"Cancellation reason must be {CancellationReasonMaxLength} characters or fewer.",
                nameof(reason));

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = trimmed;
    }

    /// <summary>
    /// True when this order held inventory reservation that must be released on cancel
    /// (Confirmed or Preparing — not yet OutForDelivery).
    /// </summary>
    public bool ShouldReleaseReservationOnCancel =>
        ConfirmedAtUtc is not null && OutForDeliveryAtUtc is null;

    /// <summary>Replace Pending items and reprice (CURRENT catalog values supplied by Application).</summary>
    public void ReplacePendingItems(
        Guid deliveryZoneId,
        string deliveryZoneName,
        decimal shippingFee,
        IReadOnlyList<OrderItemSeed> items,
        Guid? staffUserId,
        string? modificationReason,
        DateTime utcNow)
    {
        if (Status != OrderStatus.PendingConfirmation)
            throw new InvalidOperationException("Only PendingConfirmation orders can be modified.");

        EnsureUtc(utcNow, nameof(utcNow));
        if (items is null || items.Count == 0)
            throw new ArgumentException("Order requires at least one item.", nameof(items));
        if (deliveryZoneId == Guid.Empty)
            throw new ArgumentException("Delivery zone is required.", nameof(deliveryZoneId));
        if (shippingFee < 0m)
            throw new ArgumentOutOfRangeException(nameof(shippingFee));

        var before = $"items={_items.Count};subtotal={MerchandiseSubtotal};total={Total}";

        _items.Clear();
        DeliveryZoneId = deliveryZoneId;
        DeliveryZoneName = RequireText(deliveryZoneName, ZoneNameMaxLength, nameof(deliveryZoneName));
        ShippingFee = Money.Round(shippingFee);

        foreach (var seed in items)
        {
            var lineTotal = Money.Round(seed.UnitPrice * seed.Quantity);
            _items.Add(OrderItem.Create(
                Id,
                seed.ProductId,
                seed.ProductVariantId,
                seed.ProductName,
                seed.VariantName,
                seed.Sku,
                seed.SellingUnit,
                seed.Quantity,
                Money.Round(seed.UnitPrice),
                lineTotal));
        }

        RecalculateTotals();
        if (MerchandiseSubtotal < AppliedMinimumOrderAmount)
            throw new InvalidOperationException("Merchandise subtotal is below the applied minimum order amount.");

        if (staffUserId is Guid actor && actor != Guid.Empty)
        {
            var reason = string.IsNullOrWhiteSpace(modificationReason) ? "Staff modification" : modificationReason;
            var after = $"items={_items.Count};subtotal={MerchandiseSubtotal};total={Total}";
            _modificationAudits.Add(OrderModificationAudit.Create(
                Id,
                actor,
                reason,
                $"Before: {before}. After: {after}.",
                utcNow));
        }
    }

    private void RecalculateTotals()
    {
        MerchandiseSubtotal = Money.Round(_items.Sum(i => i.LineTotal));
        Total = Money.Round(MerchandiseSubtotal + ShippingFee);
    }

    private static string RequireText(string value, int max, string param)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", param);
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new ArgumentException($"Value must be {max} characters or fewer.", param);
        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new ArgumentException($"Value must be {max} characters or fewer.");
        return trimmed;
    }

    private static void EnsureUtc(DateTime value, string param)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC.", param);
    }
}

public sealed record OrderItemSeed(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal Quantity,
    decimal UnitPrice);

public static class Money
{
    public const int Precision = 18;
    public const int Scale = 2;

    public static decimal Round(decimal value) =>
        decimal.Round(value, Scale, MidpointRounding.AwayFromZero);
}
