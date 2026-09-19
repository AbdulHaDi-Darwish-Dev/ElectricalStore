using Microsoft.Extensions.Logging.Abstractions;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Inventory;
using ElectricalStore.Domain.Inventory;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Inventory;

public sealed class AdjustInventoryUseCaseTests
{
    private static readonly Guid VariantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid ActorId = Guid.Parse("99999999-8888-7777-6666-555555555555");

    [Fact]
    public async Task MissingInventory_TreatedAsZero_ThenPositiveAdjustmentCreatesRow()
    {
        InventoryItem? stored = null;
        InventoryAdjustment? audit = null;

        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.VariantExistsAsync(VariantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        inventory.Setup(i => i.GetTrackedByVariantIdAsync(VariantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        inventory.Setup(i => i.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((item, _) => stored = item)
            .Returns(Task.CompletedTask);
        inventory.Setup(i => i.AddAdjustmentAsync(It.IsAny<InventoryAdjustment>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryAdjustment, CancellationToken>((adj, _) => audit = adj)
            .Returns(Task.CompletedTask);
        inventory.Setup(i => i.GetAdminRowByVariantIdAsync(VariantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new InventoryListRow(
                Guid.NewGuid(), "Product", VariantId, "Variant", "SKU-1", "Piece",
                stored!.OnHand, stored.Reserved));

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var clock = new Mock<IAppClock>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc));

        var useCase = new AdjustInventoryUseCase(inventory.Object, uow.Object, clock.Object, NullLogger<AdjustInventoryUseCase>.Instance);
        var result = await useCase.ExecuteAsync(
            VariantId,
            new AdjustInventoryRequest(25m, "Initial stock"),
            ActorId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(stored);
        Assert.Equal(25m, stored!.OnHand);
        Assert.NotNull(audit);
        Assert.Equal(0m, audit!.OnHandBefore);
        Assert.Equal(25m, audit.OnHandAfter);
        Assert.Equal(ActorId, audit.PerformedByUserId);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ZeroAdjustment_Rejected()
    {
        var inventory = new Mock<IInventoryRepository>();
        var useCase = new AdjustInventoryUseCase(
            inventory.Object,
            Mock.Of<IAppUnitOfWork>(),
            Mock.Of<IAppClock>(), NullLogger<AdjustInventoryUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            VariantId,
            new AdjustInventoryRequest(0m, "noop"),
            ActorId);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.ZeroAdjustment.Code, result.Error!.Code);
        inventory.Verify(i => i.VariantExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingVariant_NotFound()
    {
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.VariantExistsAsync(VariantId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var useCase = new AdjustInventoryUseCase(
            inventory.Object,
            Mock.Of<IAppUnitOfWork>(),
            Mock.Of<IAppClock>(), NullLogger<AdjustInventoryUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            VariantId,
            new AdjustInventoryRequest(1m, "stock"),
            ActorId);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.VariantNotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task CannotReduceBelowReserved_MapsConflict()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.Reserve(4m);

        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.VariantExistsAsync(VariantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        inventory.Setup(i => i.GetTrackedByVariantIdAsync(VariantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var useCase = new AdjustInventoryUseCase(
            inventory.Object,
            Mock.Of<IAppUnitOfWork>(),
            Mock.Of<IAppClock>(), NullLogger<AdjustInventoryUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            VariantId,
            new AdjustInventoryRequest(-7m, "too much"),
            ActorId);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.OnHandBelowReserved.Code, result.Error!.Code);
    }

    [Fact]
    public async Task ConcurrencyConflict_MapsToResult()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(5m);

        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(i => i.VariantExistsAsync(VariantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        inventory.Setup(i => i.GetTrackedByVariantIdAsync(VariantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        inventory.Setup(i => i.AddAdjustmentAsync(It.IsAny<InventoryAdjustment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InventoryConcurrencyConflictException("conflict"));

        var clock = new Mock<IAppClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);

        var useCase = new AdjustInventoryUseCase(inventory.Object, uow.Object, clock.Object, NullLogger<AdjustInventoryUseCase>.Instance);
        var result = await useCase.ExecuteAsync(
            VariantId,
            new AdjustInventoryRequest(1m, "retry"),
            ActorId);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.ConcurrencyConflict.Code, result.Error!.Code);
    }

    [Fact]
    public async Task ReasonRequired_Rejected()
    {
        var useCase = new AdjustInventoryUseCase(
            Mock.Of<IInventoryRepository>(),
            Mock.Of<IAppUnitOfWork>(),
            Mock.Of<IAppClock>(), NullLogger<AdjustInventoryUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            VariantId,
            new AdjustInventoryRequest(1m, "   "),
            ActorId);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.ReasonRequired.Code, result.Error!.Code);
    }
}
