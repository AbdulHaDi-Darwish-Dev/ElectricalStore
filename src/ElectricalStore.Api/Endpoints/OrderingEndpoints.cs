using ElectricalStore.Api.Http;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Ordering;
using ElectricalStore.Domain.Ordering;
using Permixa.AspNetCore.Authorization;
using Permixa.AspNetCore.Security;

namespace ElectricalStore.Api.Endpoints;

public static class OrderingEndpoints
{
    public const string GuestOrderTokenHeader = "X-Order-Token";
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        MapCheckout(app);
        MapCustomerOrders(app);
        MapAdminOrders(app);
        return app;
    }

    private static void MapCheckout(IEndpointRouteBuilder app)
    {
        var checkout = app.MapGroup("/checkout")
            .WithTags("Checkout")
            .AllowAnonymous();

        checkout.MapPost("/preview", async (
                CheckoutPreviewRequest request,
                CheckoutPreviewUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("CheckoutPreview")
            .WithSummary("Preview checkout totals using current catalog/stock/shipping (non-persisting)");
    }

    private static void MapCustomerOrders(IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders")
            .WithTags("Orders");

        orders.MapPost("/", async (
                PlaceOrderRequest request,
                PlaceOrderUseCase useCase,
                ICurrentUser currentUser,
                HttpRequest http,
                CancellationToken cancellationToken) =>
            {
                Guid? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;
                string? idempotencyKey = null;
                if (http.Headers.TryGetValue(IdempotencyKeyHeader, out var values))
                    idempotencyKey = values.FirstOrDefault();

                var result = await useCase.ExecuteAsync(request, userId, idempotencyKey, cancellationToken);
                return result.ToHttpResult(created => Results.Created($"/orders/{created.Id}", created));
            })
            .AllowAnonymous()
            .WithName("PlaceOrder")
            .WithSummary("Place order (guest or authenticated). Requires Idempotency-Key. Does not reserve inventory.");

        orders.MapGet("/", async (
                ListCustomerOrdersUseCase useCase,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteAsync(currentUser.UserId.Value, cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("ListMyOrders")
            .WithSummary("List authenticated customer orders");

        orders.MapGet("/{id:guid}", async (
                Guid id,
                GetOrderByIdUseCase useCase,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteForCustomerAsync(id, currentUser.UserId.Value, cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("GetMyOrderById")
            .WithSummary("Get own order by id");

        orders.MapGet("/{id:guid}/track", async (
                Guid id,
                GetOrderByIdUseCase useCase,
                HttpRequest http,
                CancellationToken cancellationToken) =>
            {
                if (!http.Headers.TryGetValue(GuestOrderTokenHeader, out var values)
                    || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
                {
                    return Results.Problem(
                        detail: OrderingErrors.InvalidGuestToken.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: OrderingErrors.InvalidGuestToken.Code);
                }

                var result = await useCase.ExecuteForGuestAsync(id, values.First()!, cancellationToken);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("TrackGuestOrder")
            .WithSummary("Track guest order with X-Order-Token");

        orders.MapPost("/{id:guid}/cancel", async (
                Guid id,
                CancelOrderRequest? request,
                CancelOrderUseCase useCase,
                ICurrentUser currentUser,
                HttpRequest http,
                CancellationToken cancellationToken) =>
            {
                Guid? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;
                string? guestToken = null;
                if (http.Headers.TryGetValue(GuestOrderTokenHeader, out var values))
                    guestToken = values.FirstOrDefault();

                var result = await useCase.ExecuteAsCustomerAsync(
                    id,
                    userId,
                    guestToken,
                    request?.Reason,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("CancelMyOrder")
            .WithSummary("Cancel own PendingConfirmation order");

        orders.MapPut("/{id:guid}/items", async (
                Guid id,
                ModifyPendingOrderRequest request,
                ModifyPendingOrderUseCase useCase,
                ICurrentUser currentUser,
                HttpRequest http,
                CancellationToken cancellationToken) =>
            {
                Guid? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;
                string? guestToken = null;
                if (http.Headers.TryGetValue(GuestOrderTokenHeader, out var values))
                    guestToken = values.FirstOrDefault();

                var result = await useCase.ExecuteAsync(
                    id,
                    request,
                    userId,
                    guestToken,
                    staffUserId: null,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("ModifyMyPendingOrder")
            .WithSummary("Modify PendingConfirmation order items (reprices with current catalog)");
    }

    private static void MapAdminOrders(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/orders")
            .WithTags("Back Office - Orders")
            .RequireAuthorization();

        admin.MapGet("/", async (
                string? status,
                string? paymentStatus,
                string? search,
                DateTime? createdFromUtc,
                DateTime? createdToUtc,
                ListAdminOrdersUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                OrderStatus? statusEnum = null;
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var s))
                    statusEnum = s;

                PaymentStatus? payEnum = null;
                if (!string.IsNullOrWhiteSpace(paymentStatus) && Enum.TryParse<PaymentStatus>(paymentStatus, true, out var p))
                    payEnum = p;

                var list = await useCase.ExecuteAsync(
                    new OrderAdminListFilter(statusEnum, payEnum, search, createdFromUtc, createdToUtc),
                    cancellationToken);
                return Results.Ok(list);
            })
            .RequirePermission(AppPermissions.Orders.Read)
            .WithName("ListAdminOrders")
            .WithSummary("List orders");

        admin.MapGet("/{id:guid}", async (
                Guid id,
                GetOrderByIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteForAdminAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Read)
            .WithName("GetAdminOrderById")
            .WithSummary("Get order by id");

        admin.MapPost("/{id:guid}/confirm", async (
                Guid id,
                ConfirmOrderUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("ConfirmOrder")
            .WithSummary("Confirm pending order and reserve inventory atomically");

        admin.MapPost("/{id:guid}/prepare", async (
                Guid id,
                PrepareOrderUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("PrepareOrder")
            .WithSummary("Mark confirmed order as Preparing");

        admin.MapPost("/{id:guid}/out-for-delivery", async (
                Guid id,
                OutForDeliveryOrderUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("OutForDeliveryOrder")
            .WithSummary("Dispatch reserved stock and mark OutForDelivery");

        admin.MapPost("/{id:guid}/deliver", async (
                Guid id,
                DeliverOrderUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("DeliverOrder")
            .WithSummary("Mark order Delivered");

        admin.MapPost("/{id:guid}/mark-paid", async (
                Guid id,
                MarkOrderPaidUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("MarkOrderPaid")
            .WithSummary("Mark COD order as Paid");

        admin.MapPost("/{id:guid}/cancel", async (
                Guid id,
                CancelOrderRequest request,
                CancelOrderUseCase useCase,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteAsAdminAsync(
                    id,
                    currentUser.UserId.Value,
                    request.Reason,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("AdminCancelOrder")
            .WithSummary("Cancel order (releases reservation when Confirmed/Preparing)");

        admin.MapPut("/{id:guid}/items", async (
                Guid id,
                ModifyPendingOrderRequest request,
                ModifyPendingOrderUseCase useCase,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteAsync(
                    id,
                    request,
                    customerUserId: null,
                    guestRawToken: null,
                    staffUserId: currentUser.UserId.Value,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Orders.Manage)
            .WithName("AdminModifyPendingOrder")
            .WithSummary("Staff-modify PendingConfirmation order (reprices; audits reason)");
    }
}
