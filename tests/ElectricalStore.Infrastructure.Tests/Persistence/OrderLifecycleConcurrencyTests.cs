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
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.Infrastructure.Tests.Persistence;

/// <summary>Real SQL Server Order.RowVersion race tests (same Pending order).</summary>
public sealed class OrderLifecycleConcurrencyTests : IAsyncLifetime
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

    private async Task<(Guid OrderId, Guid VariantId, Guid ZoneId, Guid ProductId, string Sku)> SeedPendingOrderAsync(
        DbContextOptions<AppDbContext> options,
        decimal onHand,
        decimal orderQty = 1m)
    {
        await using var setup = new AppDbContext(options);
        await setup.Database.MigrateAsync();

        if (!await setup.OrderingSettings.AnyAsync())
            setup.OrderingSettings.Add(OrderingSettings.CreateDefault(0m));

        var category = Category.Create("Race Cat " + Guid.NewGuid().ToString("N")[..6], null, true);
        category.SetImage("key", "https://img/cat.jpg");
        setup.Categories.Add(category);

        var sku = "RACE-" + Guid.NewGuid().ToString("N")[..8];
        var product = Product.Create(
            "Race Product",
            null,
            category.Id,
            true,
            [new ProductVariantSeed("Std", sku, 100m, SellingUnit.Piece, 1m, true)]);
        product.AddImage("pkey", "https://img/p.jpg");
        setup.Products.Add(product);
        await setup.SaveChangesAsync();

        var variantId = product.Variants.Single().Id;
        var inventory = InventoryItem.CreateZero(variantId);
        inventory.AdjustOnHand(onHand);
        setup.InventoryItems.Add(inventory);

        var zone = DeliveryZone.Create("Race Zone " + Guid.NewGuid().ToString("N")[..6], 0m, true);
        setup.DeliveryZones.Add(zone);
        await setup.SaveChangesAsync();

        var tokens = new GuestOrderTokenService();
        var (_, hash) = tokens.CreateToken();
        var order = Order.Place(
            "ES-R-" + Guid.NewGuid().ToString("N")[..8],
            null,
            hash,
            "Guest",
            "0911111111",
            "Addr",
            null,
            zone.Id,
            zone.Name,
            zone.Fee,
            0m,
            [
                new OrderItemSeed(
                    product.Id, variantId, product.Name, "Std", sku, "Piece", orderQty, 100m)
            ],
            DateTime.UtcNow);

        setup.Orders.Add(order);
        await setup.SaveChangesAsync();
        return (order.Id, variantId, zone.Id, product.Id, sku);
    }

    [Fact]
    public async Task ConfirmVsConfirm_SamePendingOrder_ExactlyOneSucceeds_NoDoubleReservation()
    {
        var options = CreateOptions();
        var (orderId, variantId, _, _, _) = await SeedPendingOrderAsync(options, onHand: 5m, orderQty: 2m);

        async Task<Result<OrderDto>> ConfirmAsync()
        {
            await using var db = new AppDbContext(options);
            return await new ConfirmOrderUseCase(
                new OrderRepository(db),
                new InventoryRepository(db),
                new AppUnitOfWork(db),
                new SystemAppClock(), NullLogger<ConfirmOrderUseCase>.Instance).ExecuteAsync(orderId);
        }

        var results = await Task.WhenAll(ConfirmAsync(), ConfirmAsync());
        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(1, results.Count(r => r.IsFailure));
        Assert.Contains(results.Where(r => r.IsFailure), r =>
            r.Error!.Code is "Ordering.ConcurrencyConflict" or "Ordering.InvalidTransition"
                or "Ordering.ConfirmationStockConflict");

        await using var verify = new AppDbContext(options);
        var order = await verify.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        var stock = await verify.InventoryItems.AsNoTracking()
            .SingleAsync(x => x.ProductVariantId == variantId);
        Assert.Equal(5m, stock.OnHand);
        Assert.Equal(2m, stock.Reserved);
    }

    [Fact]
    public async Task ModifyVsConfirm_ExactlyOneSucceeds_ConsistentState()
    {
        var options = CreateOptions();
        var (orderId, variantId, zoneId, _, _) = await SeedPendingOrderAsync(options, onHand: 10m, orderQty: 1m);

        async Task<Result<OrderDto>> ConfirmAsync()
        {
            await using var db = new AppDbContext(options);
            return await new ConfirmOrderUseCase(
                new OrderRepository(db),
                new InventoryRepository(db),
                new AppUnitOfWork(db),
                new SystemAppClock(), NullLogger<ConfirmOrderUseCase>.Instance).ExecuteAsync(orderId);
        }

        async Task<Result<OrderDto>> ModifyAsync()
        {
            await using var db = new AppDbContext(options);
            return await new ModifyPendingOrderUseCase(
                new CheckoutPricingService(
                    new OrderCatalogQuery(db),
                    new InventoryRepository(db),
                    new DeliveryZoneRepository(db),
                    new OrderingSettingsRepository(db)),
                new OrderRepository(db),
                new GuestOrderTokenService(),
                new AppUnitOfWork(db),
                new SystemAppClock(), NullLogger<ModifyPendingOrderUseCase>.Instance).ExecuteAsync(
                orderId,
                new ModifyPendingOrderRequest(
                    [new CheckoutLineRequest(variantId, 3m)],
                    zoneId,
                    "phone approval"),
                customerUserId: null,
                guestRawToken: null,
                staffUserId: Guid.NewGuid());
        }

        var results = await Task.WhenAll(ConfirmAsync(), ModifyAsync());
        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(1, results.Count(r => r.IsFailure));
        Assert.Contains(results.Where(r => r.IsFailure), r =>
            r.Error!.Code is "Ordering.ConcurrencyConflict" or "Ordering.CannotModify"
                or "Ordering.InvalidTransition");

        await using var verify = new AppDbContext(options);
        var order = await verify.Orders.Include(o => o.Items).AsNoTracking()
            .SingleAsync(o => o.Id == orderId);
        var stock = await verify.InventoryItems.AsNoTracking()
            .SingleAsync(x => x.ProductVariantId == variantId);

        if (order.Status == OrderStatus.Confirmed)
        {
            Assert.Equal(1m, order.Items.Sum(i => i.Quantity));
            Assert.Equal(1m, stock.Reserved);
            Assert.Equal(10m, stock.OnHand);
        }
        else
        {
            Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
            Assert.Equal(3m, order.Items.Sum(i => i.Quantity));
            Assert.Equal(0m, stock.Reserved);
            Assert.Equal(10m, stock.OnHand);
        }
    }

    [Fact]
    public async Task CancelVsConfirm_ExactlyOneSucceeds_NoLeakedReservation()
    {
        var options = CreateOptions();
        var (orderId, variantId, _, _, _) = await SeedPendingOrderAsync(options, onHand: 4m, orderQty: 1m);

        async Task<Result<OrderDto>> ConfirmAsync()
        {
            await using var db = new AppDbContext(options);
            return await new ConfirmOrderUseCase(
                new OrderRepository(db),
                new InventoryRepository(db),
                new AppUnitOfWork(db),
                new SystemAppClock(), NullLogger<ConfirmOrderUseCase>.Instance).ExecuteAsync(orderId);
        }

        async Task<Result<OrderDto>> CancelAsync()
        {
            await using var db = new AppDbContext(options);
            return await new CancelOrderUseCase(
                new OrderRepository(db),
                new InventoryRepository(db),
                new GuestOrderTokenService(),
                new AppUnitOfWork(db),
                new SystemAppClock(), NullLogger<CancelOrderUseCase>.Instance).ExecuteAsAdminAsync(
                orderId,
                Guid.NewGuid(),
                "race cancel");
        }

        var results = await Task.WhenAll(ConfirmAsync(), CancelAsync());
        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(1, results.Count(r => r.IsFailure));

        await using var verify = new AppDbContext(options);
        var order = await verify.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        var stock = await verify.InventoryItems.AsNoTracking()
            .SingleAsync(x => x.ProductVariantId == variantId);

        Assert.True(
            order.Status is OrderStatus.Confirmed or OrderStatus.Cancelled,
            $"Unexpected status {order.Status}");

        if (order.Status == OrderStatus.Confirmed)
        {
            Assert.Equal(1m, stock.Reserved);
            Assert.Equal(4m, stock.OnHand);
        }
        else
        {
            Assert.Equal(0m, stock.Reserved);
            Assert.Equal(4m, stock.OnHand);
            Assert.Null(order.ConfirmedAtUtc);
        }

        // Never Cancelled with leaked reservation; never Confirmed after successful cancel with Reserved mismatch
        Assert.False(order.Status == OrderStatus.Cancelled && stock.Reserved > 0m);
    }
}
