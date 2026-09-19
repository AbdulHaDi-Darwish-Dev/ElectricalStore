using ElectricalStore.Domain.Ordering;
using Xunit;

namespace ElectricalStore.Domain.Tests.Ordering;

public sealed class OrderTests
{
    private static Order PlaceSample(decimal qty = 1m, decimal unitPrice = 100m, decimal min = 0m)
    {
        return Order.Place(
            "ES-TEST-1",
            userId: Guid.NewGuid(),
            guestAccessTokenHash: null,
            "Alice",
            "0999000000",
            "Street 1",
            null,
            Guid.NewGuid(),
            "Zone A",
            shippingFee: 50m,
            appliedMinimumOrderAmount: min,
            [
                new OrderItemSeed(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Cable",
                    "Std",
                    "SKU-1",
                    "Piece",
                    qty,
                    unitPrice)
            ],
            DateTime.UtcNow);
    }

    [Fact]
    public void Place_StartsPending_UnpaidCod()
    {
        var order = PlaceSample();
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
        Assert.Equal(PaymentMethod.CashOnDelivery, order.PaymentMethod);
        Assert.Equal(PaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal(150m, order.Total);
        Assert.Null(order.ConfirmedAtUtc);
    }

    [Fact]
    public void Lifecycle_HappyPath_Transitions()
    {
        var order = PlaceSample();
        var now = DateTime.UtcNow;

        order.Confirm(now);
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        order.MarkPreparing(now.AddMinutes(1));
        Assert.Equal(OrderStatus.Preparing, order.Status);

        order.MarkOutForDelivery(now.AddMinutes(2));
        Assert.Equal(OrderStatus.OutForDelivery, order.Status);

        order.MarkDelivered(now.AddMinutes(3));
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void InvalidTransitions_Throw()
    {
        var order = PlaceSample();
        Assert.Throws<InvalidOperationException>(() => order.MarkPreparing(DateTime.UtcNow));

        order.Confirm(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => order.Confirm(DateTime.UtcNow));
        Assert.Throws<InvalidOperationException>(() => order.MarkDelivered(DateTime.UtcNow));
    }

    [Fact]
    public void Cancel_AfterOutForDelivery_Throws()
    {
        var order = PlaceSample();
        var now = DateTime.UtcNow;
        order.Confirm(now);
        order.MarkPreparing(now);
        order.MarkOutForDelivery(now);
        Assert.Throws<InvalidOperationException>(() => order.Cancel(Guid.NewGuid(), "late", now));
    }

    [Fact]
    public void MarkPaid_OnlyAllowed_OutForDeliveryOrDelivered()
    {
        var order = PlaceSample();
        var now = DateTime.UtcNow;

        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());

        order.Confirm(now);
        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());

        order.MarkPreparing(now);
        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());

        order.MarkOutForDelivery(now);
        order.MarkPaid();
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());
    }

    [Fact]
    public void MarkPaid_Allowed_WhenDelivered()
    {
        var order = PlaceSample();
        var now = DateTime.UtcNow;
        order.Confirm(now);
        order.MarkPreparing(now);
        order.MarkOutForDelivery(now);
        order.MarkDelivered(now);

        order.MarkPaid();
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void MarkPaid_Cancelled_Forbidden()
    {
        var order = PlaceSample();
        order.Cancel(Guid.NewGuid(), "nope", DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => order.MarkPaid());
    }

    [Fact]
    public void ReplacePendingItems_AfterConfirm_Throws()
    {
        var order = PlaceSample();
        order.Confirm(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => order.ReplacePendingItems(
            order.DeliveryZoneId,
            order.DeliveryZoneName,
            order.ShippingFee,
            [
                new OrderItemSeed(
                    Guid.NewGuid(), Guid.NewGuid(), "P", "V", "S", "Piece", 1m, 10m)
            ],
            null,
            null,
            DateTime.UtcNow));
    }

    [Fact]
    public void QuantityRules_ExactIncrement()
    {
        Assert.True(QuantityRules.IsValidQuantity(1.5m, 0.5m));
        Assert.False(QuantityRules.IsValidQuantity(1.25m, 0.5m));
        Assert.False(QuantityRules.IsValidQuantity(0m, 1m));
    }
}
