using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class OrderingSettingsRepository : IOrderingSettingsRepository
{
    private readonly AppDbContext _db;

    public OrderingSettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrderingSettings> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.OrderingSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == OrderingSettings.SingletonId, cancellationToken);
        if (existing is not null)
            return existing;

        return await EnsureSingletonCoreAsync(asNoTracking: true, cancellationToken);
    }

    public async Task<OrderingSettings> GetTrackedOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.OrderingSettings
            .FirstOrDefaultAsync(x => x.Id == OrderingSettings.SingletonId, cancellationToken);
        if (existing is not null)
            return existing;

        return await EnsureSingletonCoreAsync(asNoTracking: false, cancellationToken);
    }

    /// <summary>
    /// Bootstrap singleton when migration seed was skipped (e.g. partial test DBs).
    /// Uses a dedicated SaveChanges so callers' units of work stay clean.
    /// </summary>
    private async Task<OrderingSettings> EnsureSingletonCoreAsync(bool asNoTracking, CancellationToken cancellationToken)
    {
        var created = OrderingSettings.CreateDefault(0m);
        _db.OrderingSettings.Add(created);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent ensure won; detach and reload.
            _db.Entry(created).State = EntityState.Detached;
        }

        if (asNoTracking)
        {
            return (await _db.OrderingSettings.AsNoTracking()
                .SingleAsync(x => x.Id == OrderingSettings.SingletonId, cancellationToken));
        }

        return await _db.OrderingSettings
            .SingleAsync(x => x.Id == OrderingSettings.SingletonId, cancellationToken);
    }
}

public sealed class OrderPlacementIdempotencyRepository : IOrderPlacementIdempotencyRepository
{
    private readonly AppDbContext _db;

    public OrderPlacementIdempotencyRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<OrderPlacementIdempotency?> FindAsync(
        string scope,
        string keyHash,
        CancellationToken cancellationToken = default) =>
        _db.OrderPlacementIdempotencies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Scope == scope && x.KeyHash == keyHash, cancellationToken);

    public Task AddAsync(OrderPlacementIdempotency record, CancellationToken cancellationToken = default)
    {
        _db.OrderPlacementIdempotencies.Add(record);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(OrderPlacementIdempotency record, CancellationToken cancellationToken = default)
    {
        // Find uses AsNoTracking; attach for delete when needed.
        var tracked = _db.OrderPlacementIdempotencies.Local.FirstOrDefault(x => x.Id == record.Id);
        if (tracked is null)
            _db.OrderPlacementIdempotencies.Attach(record);

        _db.OrderPlacementIdempotencies.Remove(tracked ?? record);
        return Task.CompletedTask;
    }
}
