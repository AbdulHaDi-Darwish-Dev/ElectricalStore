using Microsoft.Extensions.Logging.Abstractions;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Ordering;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Domain.Shipping;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Ordering;

public sealed class CheckoutPricingServiceTests
{
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid ZoneId = Guid.NewGuid();

    [Fact]
    public async Task ValidPreview_UsesCurrentPriceStockAndShipping()
    {
        var catalog = ReadyCatalog(unitPrice: 200m, increment: 1m);
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.GetAvailabilityMapAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, InventoryAvailability>
            {
                [VariantId] = new InventoryAvailability(VariantId, 10m, 2m)
            });

        var zones = ZoneRepo(1500m);
        var settings = Settings(0m);

        var svc = new CheckoutPricingService(
            catalog.Object, inventory.Object, zones.Object, settings.Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 2m)],
            ZoneId,
            minimumOverride: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, result.Value.MerchandiseSubtotal);
        Assert.Equal(1500m, result.Value.ShippingFee);
        Assert.Equal(1900m, result.Value.Total);
        Assert.Equal(8m, result.Value.Lines[0].AvailableQuantity);
    }

    [Fact]
    public async Task InsufficientStock_Rejected()
    {
        var svc = new CheckoutPricingService(
            ReadyCatalog(100m, 1m).Object, Availability(1m).Object, ZoneRepo(0m).Object, Settings(0m).Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 2m)], ZoneId, null);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderingErrors.InsufficientStock.Code, result.Error!.Code);
    }

    [Fact]
    public async Task InvalidIncrement_Rejected()
    {
        var svc = new CheckoutPricingService(
            ReadyCatalog(100m, 0.5m).Object, Availability(10m).Object, ZoneRepo(0m).Object, Settings(0m).Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 0.25m)], ZoneId, null);

        Assert.Equal(OrderingErrors.InvalidQuantityIncrement.Code, result.Error!.Code);
    }

    [Fact]
    public async Task InactiveZone_Rejected()
    {
        var inactive = DeliveryZone.Create("Inactive", 10m, isActive: false);
        var zones = new Mock<IDeliveryZoneRepository>();
        zones.Setup(z => z.GetByIdAsync(inactive.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactive);

        var svc = new CheckoutPricingService(
            ReadyCatalog(100m, 1m).Object, Availability(10m).Object, zones.Object, Settings(0m).Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 1m)], inactive.Id, null);

        Assert.Equal(OrderingErrors.DeliveryZoneInactive.Code, result.Error!.Code);
    }

    [Fact]
    public async Task NotCatalogReady_Rejected()
    {
        var catalog = new Mock<IOrderCatalogQuery>();
        catalog.Setup(c => c.GetCatalogLinesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, OrderCatalogLine>
            {
                [VariantId] = new OrderCatalogLine(
                    VariantId, ProductId, "P", null, "V", "S", "Piece", 1m, 10m,
                    true, false, true, true, true)
            });

        var svc = new CheckoutPricingService(
            catalog.Object, Availability(10m).Object, ZoneRepo(0m).Object, Settings(0m).Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 1m)], ZoneId, null);

        Assert.Equal(OrderingErrors.NotPurchasable.Code, result.Error!.Code);
    }

    [Fact]
    public async Task BelowMinimum_UsesPersistedSettings()
    {
        var svc = new CheckoutPricingService(
            ReadyCatalog(50m, 1m).Object, Availability(10m).Object, ZoneRepo(100m).Object, Settings(500m).Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 1m)], ZoneId, null);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.MeetsMinimumOrder);
        Assert.Equal(500m, result.Value.AppliedMinimumOrderAmount);
    }

    [Fact]
    public async Task MinimumOverride_IgnoresPersistedSettings()
    {
        var svc = new CheckoutPricingService(
            ReadyCatalog(50m, 1m).Object, Availability(10m).Object, ZoneRepo(0m).Object, Settings(500m).Object);

        var result = await svc.ResolveAsync(
            [new CheckoutLineRequest(VariantId, 1m)], ZoneId, minimumOverride: 40m);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.MeetsMinimumOrder);
        Assert.Equal(40m, result.Value.AppliedMinimumOrderAmount);
        Settings(500m); // ensure settings mock not required when override set — still injected
    }

    [Fact]
    public async Task DuplicateVariant_Rejected()
    {
        var svc = new CheckoutPricingService(
            Mock.Of<IOrderCatalogQuery>(),
            Mock.Of<IInventoryRepository>(),
            Mock.Of<IDeliveryZoneRepository>(),
            Mock.Of<IOrderingSettingsRepository>());

        var result = await svc.ResolveAsync(
            [
                new CheckoutLineRequest(VariantId, 1m),
                new CheckoutLineRequest(VariantId, 2m)
            ],
            ZoneId,
            null);

        Assert.Equal(OrderingErrors.DuplicateVariant.Code, result.Error!.Code);
    }

    private static Mock<IOrderCatalogQuery> ReadyCatalog(decimal unitPrice, decimal increment)
    {
        var catalog = new Mock<IOrderCatalogQuery>();
        catalog.Setup(c => c.GetCatalogLinesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, OrderCatalogLine>
            {
                [VariantId] = new OrderCatalogLine(
                    VariantId, ProductId, "Product", "https://img", "Std", "SKU", "Piece",
                    increment, unitPrice, true, true, true, true, true)
            });
        return catalog;
    }

    private static Mock<IInventoryRepository> Availability(decimal available)
    {
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.GetAvailabilityMapAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, InventoryAvailability>
            {
                [VariantId] = new InventoryAvailability(VariantId, available, 0m)
            });
        return inventory;
    }

    private static Mock<IDeliveryZoneRepository> ZoneRepo(decimal fee)
    {
        var zones = new Mock<IDeliveryZoneRepository>();
        zones.Setup(z => z.GetByIdAsync(ZoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DeliveryZone.Create("Zone", fee, isActive: true));
        return zones;
    }

    private static Mock<IOrderingSettingsRepository> Settings(decimal minimum)
    {
        var settings = new Mock<IOrderingSettingsRepository>();
        settings.Setup(s => s.GetOrCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderingSettings.CreateDefault(minimum));
        return settings;
    }
}

public sealed class ConfirmOrderUseCaseTests
{
    [Fact]
    public async Task InsufficientOneItem_LeavesPending_AndDoesNotReserve()
    {
        var order = Order.Place(
            "ES-1",
            Guid.NewGuid(),
            null,
            "A",
            "1",
            "addr",
            null,
            Guid.NewGuid(),
            "Z",
            0m,
            0m,
            [
                new OrderItemSeed(Guid.NewGuid(), Guid.NewGuid(), "P", "V", "S", "Piece", 2m, 10m)
            ],
            DateTime.UtcNow);

        var variantId = order.Items.Single().ProductVariantId;
        var item = InventoryItem.CreateZero(variantId);
        item.AdjustOnHand(1m);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(o => o.GetTrackedByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.GetTrackedByVariantIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem> { item });

        var uow = new Mock<IAppUnitOfWork>();
        var clock = new Mock<IAppClock>();
        clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var useCase = new ConfirmOrderUseCase(orders.Object, inventory.Object, uow.Object, clock.Object, NullLogger<ConfirmOrderUseCase>.Instance);
        var result = await useCase.ExecuteAsync(order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderingErrors.ConfirmationStockConflict.Code, result.Error!.Code);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
        Assert.Equal(0m, item.Reserved);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

public sealed class MarkOrderPaidUseCaseTests
{
    [Fact]
    public async Task Pending_MarkPaid_Rejected()
    {
        var order = Order.Place(
            "ES-P", Guid.NewGuid(), null, "A", "1", "addr", null,
            Guid.NewGuid(), "Z", 0m, 0m,
            [new OrderItemSeed(Guid.NewGuid(), Guid.NewGuid(), "P", "V", "S", "Piece", 1m, 10m)],
            DateTime.UtcNow);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(o => o.GetTrackedByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var useCase = new MarkOrderPaidUseCase(orders.Object, Mock.Of<IAppUnitOfWork>(), NullLogger<MarkOrderPaidUseCase>.Instance);
        var result = await useCase.ExecuteAsync(order.Id);

        Assert.Equal(OrderingErrors.InvalidTransition.Code, result.Error!.Code);
        Assert.Equal(PaymentStatus.Unpaid, order.PaymentStatus);
    }
}
