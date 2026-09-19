using ElectricalStore.Api.Http;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Inventory;
using Permixa.AspNetCore.Authorization;
using Permixa.AspNetCore.Security;

namespace ElectricalStore.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/inventory")
            .WithTags("Back Office - Inventory")
            .RequireAuthorization();

        admin.MapGet("/", async (
                Guid? productId,
                Guid? categoryId,
                string? search,
                bool? inStock,
                ListAdminInventoryUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(
                    new InventoryListFilter(productId, categoryId, search, inStock),
                    cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListAdminInventory")
            .WithSummary("List inventory by variant")
            .RequirePermission(AppPermissions.Inventory.Read);

        admin.MapGet("/{variantId:guid}", async (
                Guid variantId,
                GetAdminInventoryByVariantIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(variantId, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetAdminInventoryByVariantId")
            .WithSummary("Get inventory for a variant")
            .RequirePermission(AppPermissions.Inventory.Read);

        admin.MapGet("/{variantId:guid}/adjustments", async (
                Guid variantId,
                ListInventoryAdjustmentsUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(variantId, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ListInventoryAdjustments")
            .WithSummary("List stock adjustment history for a variant")
            .RequirePermission(AppPermissions.Inventory.Read);

        admin.MapPost("/{variantId:guid}/adjust", async (
                Guid variantId,
                AdjustInventoryRequest request,
                AdjustInventoryUseCase useCase,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteAsync(
                    variantId,
                    request,
                    currentUser.UserId.Value,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("AdjustInventory")
            .WithSummary("Adjust on-hand stock for a variant")
            .RequirePermission(AppPermissions.Inventory.Adjust);

        return app;
    }
}
