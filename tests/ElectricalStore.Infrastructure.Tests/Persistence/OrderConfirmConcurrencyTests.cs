using Microsoft.Extensions.Logging.Abstractions;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Ordering;
using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Domain.Shipping;
using ElectricalStore.Infrastructure.Ordering;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.Infrastructure.Tests.Persistence;

public sealed class OrderConfirmConcurrencyTests : IAsyncLifetime
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

    [Fact]
    public async Task ConcurrentConfirm_StockOne_TwoPendingOrders_ExactlyOneSucceeds()
    {
        var options = CreateOptions();
        Guid orderAId;
        Guid orderBId;
        Guid variantId;

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();

            var category = Category.Create("Confirm Cat", null, true);
            category.SetImage("key", "https://img/cat.jpg");
            setup.Categories.Add(category);

            var product = Product.Create(
                "Confirm Product",
                null,
                category.Id,
                true,
                [new ProductVariantSeed("Std", "CNF-" + Guid.NewGuid().ToString("N")[..8], 100m, SellingUnit.Piece, 1m, true)]);
            product.AddImage("pkey", "https://img/p.jpg");
            setup.Products.Add(product);
            await setup.SaveChangesAsync();

            variantId = product.Variants.Single().Id;

            var inventory = InventoryItem.CreateZero(variantId);
            inventory.AdjustOnHand(1m);
            setup.InventoryItems.Add(inventory);

            var zone = DeliveryZone.Create("Confirm Zone", 500m, true);
            setup.DeliveryZones.Add(zone);
            await setup.SaveChangesAsync();

            var tokens = new GuestOrderTokenService();
            var (_, hashA) = tokens.CreateToken();
            var (_, hashB) = tokens.CreateToken();

            var seed = new OrderItemSeed(
                product.Id,
                variantId,
                product.Name,
                "Std",
                product.Variants.Single().Sku,
                "Piece",
                1m,
                100m);

            var orderA = Order.Place(
                "ES-A-" + Guid.NewGuid().ToString("N")[..8],
                null,
                hashA,
                "Guest A",
                "0911111111",
                "Addr A",
                null,
                zone.Id,
                zone.Name,
                zone.Fee,
                0m,
                [seed],
                DateTime.UtcNow);

            var orderB = Order.Place(
                "ES-B-" + Guid.NewGuid().ToString("N")[..8],
                null,
                hashB,
                "Guest B",
                "0922222222",
                "Addr B",
                null,
                zone.Id,
                zone.Name,
                zone.Fee,
                0m,
                [seed],
                DateTime.UtcNow);

            setup.Orders.Add(orderA);
            setup.Orders.Add(orderB);
            await setup.SaveChangesAsync();

            orderAId = orderA.Id;
            orderBId = orderB.Id;
        }

        async Task<Result<OrderDto>> ConfirmAsync(Guid orderId)
        {
            await using var db = new AppDbContext(options);
            var useCase = new ConfirmOrderUseCase(
                new OrderRepository(db),
                new InventoryRepository(db),
                new AppUnitOfWork(db),
                new SystemAppClock(),
                NullLogger<ConfirmOrderUseCase>.Instance);
            return await useCase.ExecuteAsync(orderId);
        }

        var results = await Task.WhenAll(ConfirmAsync(orderAId), ConfirmAsync(orderBId));

        var successes = results.Count(r => r.IsSuccess);
        var failures = results.Count(r => r.IsFailure);
        Assert.Equal(1, successes);
        Assert.Equal(1, failures);
        Assert.Contains(results.Where(r => r.IsFailure), r =>
            r.Error!.Code is "Ordering.ConfirmationStockConflict" or "Ordering.ConcurrencyConflict");

        await using var verify = new AppDbContext(options);
        var item = await verify.InventoryItems.AsNoTracking()
            .SingleAsync(x => x.ProductVariantId == variantId);
        Assert.Equal(1m, item.OnHand);
        Assert.Equal(1m, item.Reserved);
        Assert.Equal(0m, item.Available);

        var statuses = await verify.Orders.AsNoTracking()
            .Where(o => o.Id == orderAId || o.Id == orderBId)
            .Select(o => o.Status)
            .ToListAsync();
        Assert.Contains(OrderStatus.Confirmed, statuses);
        Assert.Contains(OrderStatus.PendingConfirmation, statuses);
    }

    [Fact]
    public async Task PlaceOrder_DoesNotReserveInventory()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var category = Category.Create("Place Cat", null, true);
        category.SetImage("k", "https://img/c.jpg");
        db.Categories.Add(category);

        var product = Product.Create(
            "Place Product",
            null,
            category.Id,
            true,
            [new ProductVariantSeed("Std", "PLC-" + Guid.NewGuid().ToString("N")[..8], 50m, SellingUnit.Piece, 1m, true)]);
        product.AddImage("pk", "https://img/p.jpg");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var variantId = product.Variants.Single().Id;
        var inv = InventoryItem.CreateZero(variantId);
        inv.AdjustOnHand(5m);
        db.InventoryItems.Add(inv);

        var zone = DeliveryZone.Create("Place Zone", 10m, true);
        db.DeliveryZones.Add(zone);
        await db.SaveChangesAsync();

        var useCase = new PlaceOrderUseCase(
            new CheckoutPricingService(
                new OrderCatalogQuery(db),
                new InventoryRepository(db),
                new DeliveryZoneRepository(db),
                new OrderingSettingsRepository(db)),
            new OrderRepository(db),
            new OrderPlacementIdempotencyRepository(db),
            new GuestOrderTokenService(),
            new DataProtectionGuestAccessTokenProtector(new EphemeralDataProtectionProvider()),
            new AppUnitOfWork(db),
            new SystemAppClock(),
            NullLogger<PlaceOrderUseCase>.Instance);

        var key = "idem-" + Guid.NewGuid().ToString("N");
        var result = await useCase.ExecuteAsync(
            new PlaceOrderRequest(
                [new CheckoutLineRequest(variantId, 2m)],
                zone.Id,
                "Guest",
                "0933333333",
                "Addr",
                null),
            authenticatedUserId: null,
            idempotencyKey: key);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.GuestAccessToken));
        Assert.Equal("PendingConfirmation", result.Value.Status);

        var idemRow = await db.OrderPlacementIdempotencies.AsNoTracking().SingleAsync();
        Assert.DoesNotContain(result.Value.GuestAccessToken!, idemRow.ProtectedGuestAccessToken!);

        var replay = await useCase.ExecuteAsync(
            new PlaceOrderRequest(
                [new CheckoutLineRequest(variantId, 2m)],
                zone.Id,
                "Guest",
                "0933333333",
                "Addr",
                null),
            authenticatedUserId: null,
            idempotencyKey: key);

        Assert.True(replay.IsSuccess);
        Assert.Equal(result.Value.Id, replay.Value.Id);
        Assert.Equal(result.Value.GuestAccessToken, replay.Value.GuestAccessToken);
        Assert.Equal(1, await db.Orders.CountAsync());

        var stored = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == result.Value.Id);
        Assert.False(string.IsNullOrWhiteSpace(stored.GuestAccessTokenHash));
        Assert.DoesNotContain(result.Value.GuestAccessToken!, stored.GuestAccessTokenHash);

        var stock = await db.InventoryItems.AsNoTracking().SingleAsync(x => x.ProductVariantId == variantId);
        Assert.Equal(5m, stock.OnHand);
        Assert.Equal(0m, stock.Reserved);
    }
}
