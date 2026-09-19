using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        OrdersWithDetails().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Order?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        OrdersWithDetails().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await OrdersWithDetails().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListAdminAsync(
        OrderAdminListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = OrdersWithDetails().AsNoTracking().AsQueryable();

        if (filter.Status is OrderStatus status)
            query = query.Where(x => x.Status == status);

        if (filter.PaymentStatus is PaymentStatus paymentStatus)
            query = query.Where(x => x.PaymentStatus == paymentStatus);

        if (filter.CreatedFromUtc is DateTime from)
            query = query.Where(x => x.CreatedAtUtc >= from);

        if (filter.CreatedToUtc is DateTime to)
            query = query.Where(x => x.CreatedAtUtc <= to);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                x.OrderNumber.Contains(term)
                || x.Phone.Contains(term)
                || x.CustomerName.Contains(term));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        _db.Orders.AsNoTracking().AnyAsync(x => x.OrderNumber == orderNumber, cancellationToken);

    private IQueryable<Order> OrdersWithDetails() =>
        _db.Orders
            .Include(o => o.Items)
            .Include(o => o.ModificationAudits);
}

public sealed class OrderCatalogQuery : IOrderCatalogQuery
{
    private readonly AppDbContext _db;

    public OrderCatalogQuery(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<Guid, OrderCatalogLine>> GetCatalogLinesAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default)
    {
        if (variantIds.Count == 0)
            return new Dictionary<Guid, OrderCatalogLine>();

        var ids = variantIds.Distinct().ToList();

        var rows = await (
            from v in _db.ProductVariants.AsNoTracking()
            join p in _db.Products.AsNoTracking() on v.ProductId equals p.Id
            join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
            where ids.Contains(v.Id)
            select new
            {
                Variant = v,
                Product = p,
                Category = c,
                PrimaryImageUrl = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                ProductHasImage = p.Images.Any()
            }
        ).ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.Variant.Id,
            x => new OrderCatalogLine(
                x.Variant.Id,
                x.Product.Id,
                x.Product.Name,
                x.PrimaryImageUrl,
                x.Variant.Name,
                x.Variant.Sku,
                x.Variant.SellingUnit.ToString(),
                x.Variant.QuantityIncrement,
                x.Variant.Price,
                x.Product.IsActive,
                x.Variant.IsActive,
                x.Category.IsActive,
                !string.IsNullOrWhiteSpace(x.Category.ImageStorageKey),
                x.ProductHasImage));
    }
}
