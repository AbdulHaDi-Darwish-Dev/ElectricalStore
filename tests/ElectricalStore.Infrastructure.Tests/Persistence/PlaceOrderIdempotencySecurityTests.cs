using Microsoft.Extensions.Logging.Abstractions;
using ElectricalStore.Application.Abstractions;
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

public sealed class PlaceOrderIdempotencySecurityTests : IAsyncLifetime
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

    private static IGuestAccessTokenProtector CreateProtector() =>
        new DataProtectionGuestAccessTokenProtector(
            new EphemeralDataProtectionProvider());

    private async Task<(Guid VariantId, Guid ZoneId, PlaceOrderUseCase UseCase, AppDbContext Db)> SeedAsync(
        DbContextOptions<AppDbContext> options,
        IAppClock? clock = null)
    {
        var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var category = Category.Create("Idem Cat", null, true);
        category.SetImage("k", "https://img/c.jpg");
        db.Categories.Add(category);

        var product = Product.Create(
            "Idem Product",
            null,
            category.Id,
            true,
            [new ProductVariantSeed("Std", "IDM-" + Guid.NewGuid().ToString("N")[..8], 50m, SellingUnit.Piece, 1m, true)]);
        product.AddImage("pk", "https://img/p.jpg");
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var variantId = product.Variants.Single().Id;
        var inv = InventoryItem.CreateZero(variantId);
        inv.AdjustOnHand(20m);
        db.InventoryItems.Add(inv);

        var zone = DeliveryZone.Create("Idem Zone " + Guid.NewGuid().ToString("N")[..6], 0m, true);
        db.DeliveryZones.Add(zone);
        await db.SaveChangesAsync();

        var protector = CreateProtector();
        var useCase = new PlaceOrderUseCase(
            new CheckoutPricingService(
                new OrderCatalogQuery(db),
                new InventoryRepository(db),
                new DeliveryZoneRepository(db),
                new OrderingSettingsRepository(db)),
            new OrderRepository(db),
            new OrderPlacementIdempotencyRepository(db),
            new GuestOrderTokenService(),
            protector,
            new AppUnitOfWork(db),
            clock ?? new SystemAppClock(), NullLogger<PlaceOrderUseCase>.Instance);

        return (variantId, zone.Id, useCase, db);
    }

    [Fact]
    public async Task GuestIdempotency_StoresProtectedTokenOnly_AndReplayReturnsSameRawToken()
    {
        var options = CreateOptions();
        var (variantId, zoneId, useCase, db) = await SeedAsync(options);
        await using (db)
        {
            var key = "idem-" + Guid.NewGuid().ToString("N");
            var body = new PlaceOrderRequest(
                [new CheckoutLineRequest(variantId, 1m)],
                zoneId,
                "Guest",
                "0911111111",
                "Addr",
                null);

            var first = await useCase.ExecuteAsync(body, null, key);
            Assert.True(first.IsSuccess);
            var raw = first.Value.GuestAccessToken!;
            Assert.False(string.IsNullOrWhiteSpace(raw));

            var row = await db.OrderPlacementIdempotencies.AsNoTracking().SingleAsync();
            Assert.False(string.IsNullOrWhiteSpace(row.ProtectedGuestAccessToken));
            Assert.DoesNotContain(raw, row.ProtectedGuestAccessToken!);
            Assert.NotEqual(raw, row.ProtectedGuestAccessToken);
            Assert.True(row.ExpiresAtUtc > row.CreatedAtUtc);
            Assert.Equal(OrderPlacementIdempotency.RetentionHours, (row.ExpiresAtUtc - row.CreatedAtUtc).TotalHours);

            var order = await db.Orders.AsNoTracking().SingleAsync();
            Assert.DoesNotContain(raw, order.GuestAccessTokenHash!);

            var replay = await useCase.ExecuteAsync(body, null, key);
            Assert.True(replay.IsSuccess);
            Assert.Equal(first.Value.Id, replay.Value.Id);
            Assert.Equal(raw, replay.Value.GuestAccessToken);
            Assert.Equal(1, await db.Orders.CountAsync());
        }
    }

    [Fact]
    public async Task AuthenticatedIdempotency_NoProtectedGuestToken_ReplaysSameOrder()
    {
        var options = CreateOptions();
        var (variantId, zoneId, useCase, db) = await SeedAsync(options);
        await using (db)
        {
            var userId = Guid.NewGuid();
            var key = "idem-" + Guid.NewGuid().ToString("N");
            var body = new PlaceOrderRequest(
                [new CheckoutLineRequest(variantId, 1m)],
                zoneId,
                "Alice",
                "0922222222",
                "Home",
                null);

            var first = await useCase.ExecuteAsync(body, userId, key);
            Assert.True(first.IsSuccess);
            Assert.Null(first.Value.GuestAccessToken);

            var row = await db.OrderPlacementIdempotencies.AsNoTracking().SingleAsync();
            Assert.Null(row.ProtectedGuestAccessToken);
            Assert.StartsWith("u:", row.Scope);

            var replay = await useCase.ExecuteAsync(body, userId, key);
            Assert.True(replay.IsSuccess);
            Assert.Equal(first.Value.Id, replay.Value.Id);
            Assert.Equal(1, await db.Orders.CountAsync());
        }
    }

    [Fact]
    public void Protector_RoundTrips_OnlyThroughApplicationPath()
    {
        var protector = CreateProtector();
        var raw = "guest-token-value-abcdef";
        var protectedPayload = protector.Protect(raw);
        Assert.NotEqual(raw, protectedPayload);
        Assert.Equal(raw, protector.Unprotect(protectedPayload));
        Assert.Null(protector.Unprotect("not-a-valid-protected-payload"));
    }

    [Fact]
    public async Task ExpiredIdempotency_AllowsNewOrderWithSameKey()
    {
        var options = CreateOptions();
        var clock = new ControllableClock(DateTime.UtcNow);
        var (variantId, zoneId, useCase, db) = await SeedAsync(options, clock);
        await using (db)
        {
            var key = "idem-" + Guid.NewGuid().ToString("N");
            var body = new PlaceOrderRequest(
                [new CheckoutLineRequest(variantId, 1m)],
                zoneId,
                "Guest",
                "0933333333",
                "Addr",
                null);

            var first = await useCase.ExecuteAsync(body, null, key);
            Assert.True(first.IsSuccess);

            clock.UtcNow = clock.UtcNow.AddHours(OrderPlacementIdempotency.RetentionHours + 1);

            var second = await useCase.ExecuteAsync(body, null, key);
            Assert.True(second.IsSuccess);
            Assert.NotEqual(first.Value.Id, second.Value.Id);
            Assert.Equal(2, await db.Orders.CountAsync());
            Assert.Equal(1, await db.OrderPlacementIdempotencies.CountAsync());
        }
    }

    private sealed class ControllableClock : IAppClock
    {
        public ControllableClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; set; }
    }
}
