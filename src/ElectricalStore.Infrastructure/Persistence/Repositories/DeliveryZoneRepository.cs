using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Shipping;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class DeliveryZoneRepository : IDeliveryZoneRepository
{
    private readonly AppDbContext _db;

    public DeliveryZoneRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(DeliveryZone zone, CancellationToken cancellationToken = default)
    {
        _db.DeliveryZones.Add(zone);
        return Task.CompletedTask;
    }

    public Task<DeliveryZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.DeliveryZones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DeliveryZone?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.DeliveryZones.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.DeliveryZones.AsNoTracking()
            .Where(x => x.NormalizedName == normalizedName);

        if (excludeId is Guid id)
            query = query.Where(x => x.Id != id);

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryZone>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _db.DeliveryZones.AsNoTracking().AsQueryable();

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
