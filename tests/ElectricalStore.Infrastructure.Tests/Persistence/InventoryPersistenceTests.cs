using Microsoft.Extensions.Logging.Abstractions;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Inventory;
using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.Infrastructure.Tests.Persistence;

public sealed class InventoryPersistenceTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Task InitializeAsync() => _sql.StartAsync();

    public Task DisposeAsync() => _sql.DisposeAsync().AsTask();

    private DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sql.GetConnectionString(), sql =>
                sql.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTable))
            .Options;

    private static async Task<Guid> SeedVariantAsync(AppDbContext db)
    {
        var category = Category.Create("Inv Cat", null, true);
        db.Categories.Add(category);

        var product = Product.Create(
            "Inv Product",
            null,
            category.Id,
            true,
            [new ProductVariantSeed("Std", "INV-SKU-" + Guid.NewGuid().ToString("N")[..8], 10m, SellingUnit.Meter, 0.5m, true)]);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Variants.Single().Id;
    }

    [Fact]
    public async Task FirstAdjustment_CreatesItem_SecondUpdatesSameRow_AuditAtomic()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var variantId = await SeedVariantAsync(db);

        var actor = Guid.NewGuid();
        var clock = new SystemAppClock();
        var inventory = new InventoryRepository(db);
        var uow = new AppUnitOfWork(db);
        var useCase = new AdjustInventoryUseCase(inventory, uow, clock, NullLogger<AdjustInventoryUseCase>.Instance);

        var first = await useCase.ExecuteAsync(variantId, new AdjustInventoryRequest(10.5m, "Initial"), actor);
        Assert.True(first.IsSuccess);
        Assert.Equal(10.5m, first.Value.OnHand);
        Assert.Equal(1, await db.InventoryItems.CountAsync());

        var second = await useCase.ExecuteAsync(variantId, new AdjustInventoryRequest(-0.5m, "Damaged"), actor);
        Assert.True(second.IsSuccess);
        Assert.Equal(10m, second.Value.OnHand);
        Assert.Equal(1, await db.InventoryItems.CountAsync());
        Assert.Equal(2, await db.InventoryAdjustments.CountAsync(a => a.ProductVariantId == variantId));
    }

    [Fact]
    public async Task ConcurrentAdjustments_SecondSave_Conflict()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var variantId = await SeedVariantAsync(setup);
            var item = InventoryItem.CreateZero(variantId);
            item.AdjustOnHand(10m);
            setup.InventoryItems.Add(item);
            await setup.SaveChangesAsync();

            await using var ctxA = new AppDbContext(options);
            await using var ctxB = new AppDbContext(options);

            var a = await ctxA.InventoryItems.FirstAsync(x => x.ProductVariantId == variantId);
            var b = await ctxB.InventoryItems.FirstAsync(x => x.ProductVariantId == variantId);

            a.AdjustOnHand(1m);
            await new AppUnitOfWork(ctxA).SaveChangesAsync();

            b.AdjustOnHand(2m);
            var uowB = new AppUnitOfWork(ctxB);
            await Assert.ThrowsAsync<InventoryConcurrencyConflictException>(() => uowB.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task AvailabilityMap_MissingRowOmitted_PresentRowReturned()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var variantId = await SeedVariantAsync(db);
        var missingId = Guid.NewGuid();

        var item = InventoryItem.CreateZero(variantId);
        item.AdjustOnHand(3m);
        item.Reserve(1m);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var repo = new InventoryRepository(db);
        var map = await repo.GetAvailabilityMapAsync([variantId, missingId]);

        Assert.True(map.ContainsKey(variantId));
        Assert.Equal(2m, map[variantId].Available);
        Assert.False(map.ContainsKey(missingId));
    }
}
