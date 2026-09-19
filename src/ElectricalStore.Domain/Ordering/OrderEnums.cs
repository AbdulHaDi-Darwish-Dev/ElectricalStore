namespace ElectricalStore.Domain.Ordering;

public enum OrderStatus
{
    PendingConfirmation = 0,
    Confirmed = 1,
    Preparing = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    CashOnDelivery = 0
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1
}
