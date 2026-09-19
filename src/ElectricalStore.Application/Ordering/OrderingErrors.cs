using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Ordering;

public static class OrderingErrors
{
    public static Error EmptyItems { get; } =
        new("Ordering.EmptyItems", "At least one order item is required.");

    public static Error DuplicateVariant { get; } =
        new("Ordering.DuplicateVariant", "Duplicate product variants in the request are not allowed.");

    public static Error QuantityMustBePositive { get; } =
        new("Ordering.QuantityMustBePositive", "Quantity must be greater than zero.");

    public static Error InvalidQuantityIncrement { get; } =
        new("Ordering.InvalidQuantityIncrement", "Quantity must be a positive multiple of the variant quantity increment.");

    public static Error VariantNotFound { get; } =
        new("Ordering.VariantNotFound", "Product variant was not found.");

    public static Error NotPurchasable { get; } =
        new("Ordering.NotPurchasable", "One or more items cannot currently be purchased.");

    public static Error InsufficientStock { get; } =
        new("Ordering.InsufficientStock", "Requested quantity exceeds currently available stock.");

    public static Error DeliveryZoneNotFound { get; } =
        new("Ordering.DeliveryZoneNotFound", "Delivery zone was not found.");

    public static Error DeliveryZoneInactive { get; } =
        new("Ordering.DeliveryZoneInactive", "Delivery zone is not active.");

    public static Error BelowMinimumOrder { get; } =
        new("Ordering.BelowMinimumOrder", "Merchandise subtotal is below the minimum order amount.");

    public static Error CustomerNameRequired { get; } =
        new("Ordering.CustomerNameRequired", "Customer name is required.");

    public static Error PhoneRequired { get; } =
        new("Ordering.PhoneRequired", "Phone is required.");

    public static Error AddressRequired { get; } =
        new("Ordering.AddressRequired", "Delivery address is required.");

    public static Error IdentityRequired { get; } =
        new("Ordering.IdentityRequired", "Provide a Bearer JWT or place the order as a guest.");

    public static Error NotFound { get; } =
        new("Ordering.NotFound", "Order was not found.");

    public static Error Forbidden { get; } =
        new("Ordering.Forbidden", "You are not allowed to access this order.");

    public static Error InvalidGuestToken { get; } =
        new("Ordering.InvalidGuestToken", "Guest order token is invalid.");

    public static Error InvalidTransition { get; } =
        new("Ordering.InvalidTransition", "The requested order operation is not allowed in the current status.");

    public static Error AlreadyPaid { get; } =
        new("Ordering.AlreadyPaid", "Order is already marked paid.");

    public static Error ConfirmationStockConflict { get; } =
        new("Ordering.ConfirmationStockConflict", "Insufficient stock to confirm this order. No inventory was reserved.");

    public static Error ConcurrencyConflict { get; } =
        new("Ordering.ConcurrencyConflict", "Order or inventory was modified concurrently. Retry the operation.");

    public static Error CancellationReasonRequired { get; } =
        new("Ordering.CancellationReasonRequired", "Cancellation reason is required.");

    public static Error CannotModify { get; } =
        new("Ordering.CannotModify", "Only PendingConfirmation orders can be modified.");

    public static Error ActorRequired { get; } =
        new("Ordering.ActorRequired", "Authenticated user is required for this operation.");

    public static Error InvalidMinimumOrderAmount { get; } =
        new("Ordering.InvalidMinimumOrderAmount", "Minimum merchandise subtotal cannot be negative.");

    public static Error IdempotencyKeyRequired { get; } =
        new("Ordering.IdempotencyKeyRequired", "Idempotency-Key header is required.");

    public static Error InvalidIdempotencyKey { get; } =
        new("Ordering.InvalidIdempotencyKey", "Idempotency-Key must be 16–128 characters of opaque high-entropy text.");

    public static Error IdempotencyReplayUnavailable { get; } =
        new("Ordering.IdempotencyReplayUnavailable", "The previous Place Order result cannot be replayed. Retry with a new Idempotency-Key.");
}
