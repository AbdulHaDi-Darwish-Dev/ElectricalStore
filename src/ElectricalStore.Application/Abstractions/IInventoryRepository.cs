using ElectricalStore.Domain.Inventory;

namespace ElectricalStore.Application.Abstractions;

public sealed record InventoryListFilter(
    Guid? ProductId = null,
    Guid? CategoryId = null,
    string? Search = null,
    bool? InStock = null);

public sealed record InventoryAvailability(Guid ProductVariantId, decimal OnHand, decimal Reserved)
{
    public decimal Available => OnHand - Reserved;

    public bool IsInStock => Available > 0m;

    public static InventoryAvailability Zero(Guid productVariantId) =>
        new(productVariantId, 0m, 0m);
}

public sealed record InventoryListRow(
    Guid ProductId,
    string ProductName,
    Guid VariantId,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal OnHand,
    decimal Reserved);

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetTrackedByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);

    /// <summary>Tracked load of many inventory rows for atomic confirm/dispatch/cancel.</summary>
    Task<IReadOnlyList<InventoryItem>> GetTrackedByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task AddAdjustmentAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken = default);

    /// <summary>Set-based availability for many variants. Missing rows are omitted (caller treats as zero).</summary>
    Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityMapAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryListRow>> ListAdminAsync(
        InventoryListFilter filter,
        CancellationToken cancellationToken = default);

    Task<InventoryListRow?> GetAdminRowByVariantIdAsync(
        Guid variantId,
        CancellationToken cancellationToken = default);

    Task<bool> VariantExistsAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryAdjustment>> ListAdjustmentsAsync(
        Guid variantId,
        CancellationToken cancellationToken = default);
}
