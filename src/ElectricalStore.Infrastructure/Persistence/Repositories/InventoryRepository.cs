using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;

    public InventoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<InventoryItem?> GetByVariantIdAsync(
        Guid variantId,
        CancellationToken cancellationToken = default) =>
        _db.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductVariantId == variantId, cancellationToken);

    public Task<InventoryItem?> GetTrackedByVariantIdAsync(
        Guid variantId,
        CancellationToken cancellationToken = default) =>
        _db.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductVariantId == variantId, cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> GetTrackedByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default)
    {
        if (variantIds.Count == 0)
            return Array.Empty<InventoryItem>();

        var ids = variantIds.Distinct().ToList();
        return await _db.InventoryItems
            .Where(x => ids.Contains(x.ProductVariantId))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _db.InventoryItems.Add(item);
        return Task.CompletedTask;
    }

    public Task AddAdjustmentAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        _db.InventoryAdjustments.Add(adjustment);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityMapAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default)
    {
        if (variantIds.Count == 0)
            return new Dictionary<Guid, InventoryAvailability>();

        var ids = variantIds.Distinct().ToList();
        var rows = await _db.InventoryItems.AsNoTracking()
            .Where(x => ids.Contains(x.ProductVariantId))
            .Select(x => new { x.ProductVariantId, x.OnHand, x.Reserved })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.ProductVariantId,
            x => new InventoryAvailability(x.ProductVariantId, x.OnHand, x.Reserved));
    }

    public async Task<IReadOnlyList<InventoryListRow>> ListAdminAsync(
        InventoryListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query =
            from v in _db.ProductVariants.AsNoTracking()
            join p in _db.Products.AsNoTracking() on v.ProductId equals p.Id
            join inv in _db.InventoryItems.AsNoTracking()
                on v.Id equals inv.ProductVariantId into invGroup
            from inv in invGroup.DefaultIfEmpty()
            select new
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryId = p.CategoryId,
                VariantId = v.Id,
                VariantName = v.Name,
                Sku = v.Sku,
                SellingUnit = v.SellingUnit,
                OnHand = inv != null ? inv.OnHand : 0m,
                Reserved = inv != null ? inv.Reserved : 0m
            };

        if (filter.ProductId is Guid productId)
            query = query.Where(x => x.ProductId == productId);

        if (filter.CategoryId is Guid categoryId)
            query = query.Where(x => x.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                x.ProductName.Contains(term)
                || x.VariantName.Contains(term)
                || x.Sku.Contains(term));
        }

        if (filter.InStock is true)
            query = query.Where(x => x.OnHand - x.Reserved > 0m);
        else if (filter.InStock is false)
            query = query.Where(x => x.OnHand - x.Reserved <= 0m);

        var rows = await query
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.VariantName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new InventoryListRow(
                x.ProductId,
                x.ProductName,
                x.VariantId,
                x.VariantName,
                x.Sku,
                x.SellingUnit.ToString(),
                x.OnHand,
                x.Reserved))
            .ToList();
    }

    public async Task<InventoryListRow?> GetAdminRowByVariantIdAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from v in _db.ProductVariants.AsNoTracking()
            join p in _db.Products.AsNoTracking() on v.ProductId equals p.Id
            join inv in _db.InventoryItems.AsNoTracking()
                on v.Id equals inv.ProductVariantId into invGroup
            from inv in invGroup.DefaultIfEmpty()
            where v.Id == variantId
            select new
            {
                ProductId = p.Id,
                ProductName = p.Name,
                VariantId = v.Id,
                VariantName = v.Name,
                Sku = v.Sku,
                SellingUnit = v.SellingUnit,
                OnHand = inv != null ? inv.OnHand : 0m,
                Reserved = inv != null ? inv.Reserved : 0m
            }
        ).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new InventoryListRow(
                row.ProductId,
                row.ProductName,
                row.VariantId,
                row.VariantName,
                row.Sku,
                row.SellingUnit.ToString(),
                row.OnHand,
                row.Reserved);
    }

    public Task<bool> VariantExistsAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        _db.ProductVariants.AsNoTracking().AnyAsync(x => x.Id == variantId, cancellationToken);

    public async Task<IReadOnlyList<InventoryAdjustment>> ListAdjustmentsAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        return await _db.InventoryAdjustments.AsNoTracking()
            .Where(x => x.ProductVariantId == variantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
