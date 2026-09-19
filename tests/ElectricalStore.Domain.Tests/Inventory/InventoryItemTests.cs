using ElectricalStore.Domain.Inventory;
using Xunit;

namespace ElectricalStore.Domain.Tests.Inventory;

public sealed class InventoryItemTests
{
    private static Guid VariantId => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void CreateZero_HasZeroAvailability()
    {
        var item = InventoryItem.CreateZero(VariantId);

        Assert.Equal(0m, item.OnHand);
        Assert.Equal(0m, item.Reserved);
        Assert.Equal(0m, item.Available);
    }

    [Fact]
    public void AdjustOnHand_Positive_IncreasesStock()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(25.5m);

        Assert.Equal(25.5m, item.OnHand);
        Assert.Equal(25.5m, item.Available);
    }

    [Fact]
    public void AdjustOnHand_Negative_DecreasesStock()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.AdjustOnHand(-2.5m);

        Assert.Equal(7.5m, item.OnHand);
        Assert.Equal(7.5m, item.Available);
    }

    [Fact]
    public void AdjustOnHand_Zero_Throws()
    {
        var item = InventoryItem.CreateZero(VariantId);
        Assert.Throws<ArgumentException>(() => item.AdjustOnHand(0m));
    }

    [Fact]
    public void AdjustOnHand_CannotMakeOnHandNegative()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(5m);

        var ex = Assert.Throws<InvalidOperationException>(() => item.AdjustOnHand(-6m));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5m, item.OnHand);
    }

    [Fact]
    public void AdjustOnHand_CannotReduceBelowReserved()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.Reserve(4m);

        var ex = Assert.Throws<InvalidOperationException>(() => item.AdjustOnHand(-7m));
        Assert.Contains("cannot exceed on-hand", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(10m, item.OnHand);
        Assert.Equal(4m, item.Reserved);
    }

    [Fact]
    public void AdjustOnHand_MayReduceToReserved()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.Reserve(4m);

        item.AdjustOnHand(-6m);

        Assert.Equal(4m, item.OnHand);
        Assert.Equal(4m, item.Reserved);
        Assert.Equal(0m, item.Available);
    }

    [Fact]
    public void Available_IsDerived()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.Reserve(3.25m);

        Assert.Equal(6.75m, item.Available);
    }

    [Fact]
    public void Reserve_CannotExceedAvailable()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(5m);

        Assert.Throws<InvalidOperationException>(() => item.Reserve(5.001m));
        Assert.Equal(0m, item.Reserved);
    }

    [Fact]
    public void Reserve_SucceedsWithinAvailable()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(5m);
        item.Reserve(2m);

        Assert.Equal(2m, item.Reserved);
        Assert.Equal(3m, item.Available);
    }

    [Fact]
    public void Release_CannotExceedReserved()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(5m);
        item.Reserve(2m);

        Assert.Throws<InvalidOperationException>(() => item.Release(3m));
        Assert.Equal(2m, item.Reserved);
    }

    [Fact]
    public void Dispatch_UpdatesOnHandAndReserved()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.Reserve(4m);

        item.Dispatch(3m);

        Assert.Equal(7m, item.OnHand);
        Assert.Equal(1m, item.Reserved);
        Assert.Equal(6m, item.Available);
    }

    [Fact]
    public void Dispatch_CannotExceedReserved()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(10m);
        item.Reserve(2m);

        Assert.Throws<InvalidOperationException>(() => item.Dispatch(3m));
        Assert.Equal(10m, item.OnHand);
        Assert.Equal(2m, item.Reserved);
    }

    [Fact]
    public void DecimalIncrements_RemainPrecise()
    {
        var item = InventoryItem.CreateZero(VariantId);
        item.AdjustOnHand(1.5m);
        item.AdjustOnHand(0.25m);
        item.Reserve(0.5m);

        Assert.Equal(1.75m, item.OnHand);
        Assert.Equal(1.25m, item.Available);
    }
}
