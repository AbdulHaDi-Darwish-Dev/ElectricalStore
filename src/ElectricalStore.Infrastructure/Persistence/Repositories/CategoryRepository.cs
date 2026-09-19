using Microsoft.EntityFrameworkCore;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Infrastructure.Persistence;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _db.Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Category?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories.AsNoTracking()
            .Where(x => x.NormalizedName == normalizedName);

        if (excludeId is Guid id)
            query = query.Where(x => x.Id != id);

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListAsync(
        bool activeOnly,
        bool requireImage = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (requireImage)
            query = query.Where(x => x.ImageStorageKey != null && x.ImageStorageKey != "");

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
