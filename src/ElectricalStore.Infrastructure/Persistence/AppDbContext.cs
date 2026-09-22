using Microsoft.EntityFrameworkCore;
using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Domain.Customers;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Domain.Shipping;

namespace ElectricalStore.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public const string MigrationsHistoryTable = "__AppMigrationsHistory";

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderModificationAudit> OrderModificationAudits => Set<OrderModificationAudit>();

    public DbSet<OrderingSettings> OrderingSettings => Set<OrderingSettings>();

    public DbSet<OrderPlacementIdempotency> OrderPlacementIdempotencies => Set<OrderPlacementIdempotency>();

    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
