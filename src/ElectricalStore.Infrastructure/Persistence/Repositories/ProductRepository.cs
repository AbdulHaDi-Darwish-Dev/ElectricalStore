using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _db.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ProductsWithDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Product?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ProductsWithDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> AnyNormalizedSkuExistsAsync(
        IReadOnlyCollection<string> normalizedSkus,
        Guid? excludeVariantId = null,
        CancellationToken cancellationToken = default)
    {
        if (normalizedSkus.Count == 0)
            return false;

        var query = _db.ProductVariants.AsNoTracking()
            .Where(v => normalizedSkus.Contains(v.NormalizedSku));

        if (excludeVariantId is Guid id)
            query = query.Where(v => v.Id != id);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Product Product, string CategoryName, bool CategoryIsActive)>> ListAdminAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query =
            from p in ProductsWithDetails().AsNoTracking()
            join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
            select new { Product = p, Category = c };

        if (filter.CategoryId is Guid categoryId)
            query = query.Where(x => x.Product.CategoryId == categoryId);

        if (filter.IsActive is bool isActive)
            query = query.Where(x => x.Product.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => x.Product.Name.Contains(term));
        }

        var rows = await query
            .OrderBy(x => x.Product.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => (x.Product, x.Category.Name, x.Category.IsActive))
            .ToList();
    }

    public async Task<(Product Product, string CategoryName, bool CategoryIsActive)?> GetAdminByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from p in ProductsWithDetails().AsNoTracking()
            join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
            where p.Id == id
            select new { Product = p, Category = c }
        ).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : (row.Product, row.Category.Name, row.Category.IsActive);
    }

    public async Task<IReadOnlyList<(Product Product, string CategoryName)>> ListCatalogAsync(
        CatalogProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query =
            from p in ProductsWithDetails().AsNoTracking()
            join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
            where p.IsActive
                  && c.IsActive
                  && c.ImageStorageKey != null
                  && c.ImageStorageKey != ""
                  && p.Images.Any()
                  && p.Variants.Any(v => v.IsActive)
            select new { Product = p, Category = c };

        if (filter.CategoryId is Guid categoryId)
            query = query.Where(x => x.Product.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => x.Product.Name.Contains(term));
        }

        var rows = await query
            .OrderBy(x => x.Product.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => (x.Product, x.Category.Name))
            .ToList();
    }

    public async Task<(Product Product, string CategoryName)?> GetCatalogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from p in ProductsWithDetails().AsNoTracking()
            join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
            where p.Id == id
                  && p.IsActive
                  && c.IsActive
                  && c.ImageStorageKey != null
                  && c.ImageStorageKey != ""
                  && p.Images.Any()
                  && p.Variants.Any(v => v.IsActive)
            select new { Product = p, Category = c }
        ).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : (row.Product, row.Category.Name);
    }

    private IQueryable<Product> ProductsWithDetails() =>
        _db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images);
}
