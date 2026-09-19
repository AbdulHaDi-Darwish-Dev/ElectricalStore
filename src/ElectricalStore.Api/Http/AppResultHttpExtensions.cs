using ElectricalStore.Application.Common;

namespace ElectricalStore.Api.Http;

public static class AppResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblem(result.Error!);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        var status = MapStatus(error.Code);
        return Results.Problem(
            detail: error.Message,
            statusCode: status,
            title: error.Code,
            type: $"https://httpstatuses.com/{status}",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }

    private static int MapStatus(string code) =>
        code switch
        {
            "Category.NotFound" => StatusCodes.Status404NotFound,
            "Category.NameAlreadyExists" => StatusCodes.Status409Conflict,
            "Category.NameRequired" => StatusCodes.Status400BadRequest,
            "Category.NameTooLong" => StatusCodes.Status400BadRequest,
            "Category.DescriptionTooLong" => StatusCodes.Status400BadRequest,

            "Product.NotFound" => StatusCodes.Status404NotFound,
            "Product.VariantNotFound" => StatusCodes.Status404NotFound,
            "Product.CategoryNotFound" => StatusCodes.Status404NotFound,
            "Product.SkuAlreadyExists" => StatusCodes.Status409Conflict,
            "Product.DuplicateSkuInRequest" => StatusCodes.Status409Conflict,
            "Product.DuplicateVariantName" => StatusCodes.Status409Conflict,

            "Category.ImageNotFound" => StatusCodes.Status404NotFound,
            "Product.ImageNotFound" => StatusCodes.Status404NotFound,
            "Product.TooManyImages" => StatusCodes.Status409Conflict,

            "Media.UploadFailed" => StatusCodes.Status503ServiceUnavailable,
            "Media.NotConfigured" => StatusCodes.Status503ServiceUnavailable,

            "Inventory.VariantNotFound" => StatusCodes.Status404NotFound,
            "Inventory.ZeroAdjustment" => StatusCodes.Status400BadRequest,
            "Inventory.ReasonRequired" => StatusCodes.Status400BadRequest,
            "Inventory.ReasonTooLong" => StatusCodes.Status400BadRequest,
            "Inventory.InvalidQuantity" => StatusCodes.Status400BadRequest,
            "Inventory.ActorRequired" => StatusCodes.Status400BadRequest,
            "Inventory.OnHandWouldBeNegative" => StatusCodes.Status409Conflict,
            "Inventory.OnHandBelowReserved" => StatusCodes.Status409Conflict,
            "Inventory.ConcurrencyConflict" => StatusCodes.Status409Conflict,
            "Inventory.InsufficientAvailable" => StatusCodes.Status409Conflict,
            "Inventory.InsufficientReserved" => StatusCodes.Status409Conflict,

            "Shipping.NotFound" => StatusCodes.Status404NotFound,
            "Shipping.NameRequired" => StatusCodes.Status400BadRequest,
            "Shipping.NameTooLong" => StatusCodes.Status400BadRequest,
            "Shipping.NegativeFee" => StatusCodes.Status400BadRequest,
            "Shipping.NameAlreadyExists" => StatusCodes.Status409Conflict,

            "Ordering.EmptyItems" => StatusCodes.Status400BadRequest,
            "Ordering.DuplicateVariant" => StatusCodes.Status400BadRequest,
            "Ordering.QuantityMustBePositive" => StatusCodes.Status400BadRequest,
            "Ordering.InvalidQuantityIncrement" => StatusCodes.Status400BadRequest,
            "Ordering.CustomerNameRequired" => StatusCodes.Status400BadRequest,
            "Ordering.PhoneRequired" => StatusCodes.Status400BadRequest,
            "Ordering.AddressRequired" => StatusCodes.Status400BadRequest,
            "Ordering.IdentityRequired" => StatusCodes.Status400BadRequest,
            "Ordering.InvalidGuestToken" => StatusCodes.Status400BadRequest,
            "Ordering.CancellationReasonRequired" => StatusCodes.Status400BadRequest,
            "Ordering.ActorRequired" => StatusCodes.Status400BadRequest,
            "Ordering.VariantNotFound" => StatusCodes.Status404NotFound,
            "Ordering.DeliveryZoneNotFound" => StatusCodes.Status404NotFound,
            "Ordering.NotFound" => StatusCodes.Status404NotFound,
            "Ordering.Forbidden" => StatusCodes.Status403Forbidden,
            "Ordering.NotPurchasable" => StatusCodes.Status409Conflict,
            "Ordering.InsufficientStock" => StatusCodes.Status409Conflict,
            "Ordering.DeliveryZoneInactive" => StatusCodes.Status409Conflict,
            "Ordering.BelowMinimumOrder" => StatusCodes.Status409Conflict,
            "Ordering.InvalidTransition" => StatusCodes.Status409Conflict,
            "Ordering.AlreadyPaid" => StatusCodes.Status409Conflict,
            "Ordering.ConfirmationStockConflict" => StatusCodes.Status409Conflict,
            "Ordering.ConcurrencyConflict" => StatusCodes.Status409Conflict,
            "Ordering.CannotModify" => StatusCodes.Status409Conflict,
            "Ordering.InvalidMinimumOrderAmount" => StatusCodes.Status400BadRequest,
            "Ordering.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
            "Ordering.InvalidIdempotencyKey" => StatusCodes.Status400BadRequest,
            "Ordering.IdempotencyReplayUnavailable" => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };
}
