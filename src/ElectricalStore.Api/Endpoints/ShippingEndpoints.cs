using ElectricalStore.Api.Http;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Shipping;
using Permixa.AspNetCore.Authorization;

namespace ElectricalStore.Api.Endpoints;

public static class ShippingEndpoints
{
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        MapAdminShippingEndpoints(app);
        MapPublicShippingEndpoints(app);
        return app;
    }

    private static void MapAdminShippingEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/shipping/zones")
            .WithTags("Back Office - Shipping")
            .RequireAuthorization()
            .RequirePermission(AppPermissions.Shipping.Manage);

        admin.MapPost("/", async (
                CreateDeliveryZoneRequest request,
                CreateDeliveryZoneUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return result.ToHttpResult(created =>
                    Results.Created($"/admin/shipping/zones/{created.Id}", created));
            })
            .WithName("CreateDeliveryZone")
            .WithSummary("Create delivery zone");

        admin.MapGet("/", async (
                ListAdminDeliveryZonesUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListAdminDeliveryZones")
            .WithSummary("List delivery zones");

        admin.MapGet("/{id:guid}", async (
                Guid id,
                GetAdminDeliveryZoneByIdUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GetAdminDeliveryZoneById")
            .WithSummary("Get delivery zone by ID");

        admin.MapPut("/{id:guid}", async (
                Guid id,
                UpdateDeliveryZoneRequest request,
                UpdateDeliveryZoneUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, request, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("UpdateDeliveryZone")
            .WithSummary("Update delivery zone");

        admin.MapPost("/{id:guid}/activate", async (
                Guid id,
                ActivateDeliveryZoneUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ActivateDeliveryZone")
            .WithSummary("Activate delivery zone");

        admin.MapPost("/{id:guid}/deactivate", async (
                Guid id,
                DeactivateDeliveryZoneUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeactivateDeliveryZone")
            .WithSummary("Deactivate delivery zone");
    }

    private static void MapPublicShippingEndpoints(IEndpointRouteBuilder app)
    {
        var pub = app.MapGroup("/shipping/zones")
            .WithTags("Shipping");

        pub.MapGet("/", async (
                ListActiveDeliveryZonesUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var list = await useCase.ExecuteAsync(cancellationToken);
                return Results.Ok(list);
            })
            .WithName("ListActiveDeliveryZones")
            .WithSummary("List active delivery zones");
    }
}
