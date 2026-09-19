namespace ElectricalStore.Domain.Ordering;

/// <summary>Lightweight audit for staff edits to PendingConfirmation orders (not event sourcing).</summary>
public sealed class OrderModificationAudit
{
    public const int ReasonMaxLength = 500;
    public const int SummaryMaxLength = 2000;

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    private OrderModificationAudit()
    {
    }

    public static OrderModificationAudit Create(
        Guid orderId,
        Guid performedByUserId,
        string reason,
        string summary,
        DateTime createdAtUtc)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (performedByUserId == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(performedByUserId));
        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("CreatedAtUtc must be UTC.", nameof(createdAtUtc));

        var trimmedReason = (reason ?? string.Empty).Trim();
        if (trimmedReason.Length == 0)
            throw new ArgumentException("Reason is required.", nameof(reason));
        if (trimmedReason.Length > ReasonMaxLength)
            throw new ArgumentException($"Reason must be {ReasonMaxLength} characters or fewer.", nameof(reason));

        var trimmedSummary = (summary ?? string.Empty).Trim();
        if (trimmedSummary.Length > SummaryMaxLength)
            trimmedSummary = trimmedSummary[..SummaryMaxLength];

        return new OrderModificationAudit
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PerformedByUserId = performedByUserId,
            Reason = trimmedReason,
            Summary = trimmedSummary,
            CreatedAtUtc = createdAtUtc
        };
    }
}
