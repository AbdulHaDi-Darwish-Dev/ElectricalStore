namespace ElectricalStore.Domain.Inventory;

/// <summary>Lightweight operational audit for manual admin stock adjustments (not event sourcing).</summary>
public sealed class InventoryAdjustment
{
    public const int ReasonMaxLength = 500;

    public Guid Id { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public decimal QuantityDelta { get; private set; }

    public decimal OnHandBefore { get; private set; }

    public decimal OnHandAfter { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public Guid PerformedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private InventoryAdjustment()
    {
    }

    public static InventoryAdjustment Create(
        Guid productVariantId,
        decimal quantityDelta,
        decimal onHandBefore,
        decimal onHandAfter,
        string reason,
        Guid performedByUserId,
        DateTime createdAtUtc)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Product variant id is required.", nameof(productVariantId));
        if (quantityDelta == 0m)
            throw new ArgumentException("Quantity delta cannot be zero.", nameof(quantityDelta));
        if (performedByUserId == Guid.Empty)
            throw new ArgumentException("Performed-by user id is required.", nameof(performedByUserId));
        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("CreatedAtUtc must be UTC.", nameof(createdAtUtc));

        var trimmedReason = NormalizeReason(reason);

        return new InventoryAdjustment
        {
            Id = Guid.NewGuid(),
            ProductVariantId = productVariantId,
            QuantityDelta = quantityDelta,
            OnHandBefore = onHandBefore,
            OnHandAfter = onHandAfter,
            Reason = trimmedReason,
            PerformedByUserId = performedByUserId,
            CreatedAtUtc = createdAtUtc
        };
    }

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
            throw new ArgumentException(
                $"Reason must be {ReasonMaxLength} characters or fewer.",
                nameof(reason));

        return trimmed;
    }
}
