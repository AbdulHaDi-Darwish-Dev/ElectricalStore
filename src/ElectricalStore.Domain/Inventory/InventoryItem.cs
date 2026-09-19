namespace ElectricalStore.Domain.Inventory;

/// <summary>
/// Stock state for a single ProductVariant. Missing row means zero stock (see Application docs).
/// Quantities use the same scale as variant increments (SQL decimal(18,3)).
/// </summary>
public sealed class InventoryItem
{
    public const int QuantityPrecision = 18;
    public const int QuantityScale = 3;

    public Guid ProductVariantId { get; private set; }

    public decimal OnHand { get; private set; }

    public decimal Reserved { get; private set; }

    /// <summary>SQL Server rowversion concurrency token. Managed by EF; not set by domain logic.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public decimal Available => OnHand - Reserved;

    private InventoryItem()
    {
    }

    public static InventoryItem CreateZero(Guid productVariantId)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Product variant id is required.", nameof(productVariantId));

        return new InventoryItem
        {
            ProductVariantId = productVariantId,
            OnHand = 0m,
            Reserved = 0m
        };
    }

    /// <summary>Admin stock change. Does not mutate Reserved.</summary>
    public void AdjustOnHand(decimal quantityDelta)
    {
        if (quantityDelta == 0m)
            throw new ArgumentException("Quantity delta cannot be zero.", nameof(quantityDelta));

        var next = OnHand + quantityDelta;
        EnsureInvariants(next, Reserved);
        OnHand = next;
    }

    /// <summary>Order-confirmation reservation primitive (not an HTTP admin operation).</summary>
    public void Reserve(decimal quantity)
    {
        EnsurePositiveQuantity(quantity);
        if (quantity > Available)
            throw new InvalidOperationException("Cannot reserve more than available quantity.");

        var nextReserved = Reserved + quantity;
        EnsureInvariants(OnHand, nextReserved);
        Reserved = nextReserved;
    }

    /// <summary>Release a previous reservation (cancel / timeout).</summary>
    public void Release(decimal quantity)
    {
        EnsurePositiveQuantity(quantity);
        if (quantity > Reserved)
            throw new InvalidOperationException("Cannot release more than reserved quantity.");

        var nextReserved = Reserved - quantity;
        EnsureInvariants(OnHand, nextReserved);
        Reserved = nextReserved;
    }

    /// <summary>Consume reserved stock when fulfilling (OnHand and Reserved both decrease).</summary>
    public void Dispatch(decimal quantity)
    {
        EnsurePositiveQuantity(quantity);
        if (quantity > Reserved)
            throw new InvalidOperationException("Cannot dispatch more than reserved quantity.");

        var nextOnHand = OnHand - quantity;
        var nextReserved = Reserved - quantity;
        EnsureInvariants(nextOnHand, nextReserved);
        OnHand = nextOnHand;
        Reserved = nextReserved;
    }

    private static void EnsurePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0m)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
    }

    private static void EnsureInvariants(decimal onHand, decimal reserved)
    {
        if (onHand < 0m)
            throw new InvalidOperationException("On-hand quantity cannot be negative.");
        if (reserved < 0m)
            throw new InvalidOperationException("Reserved quantity cannot be negative.");
        if (reserved > onHand)
            throw new InvalidOperationException("Reserved quantity cannot exceed on-hand quantity.");
    }
}
