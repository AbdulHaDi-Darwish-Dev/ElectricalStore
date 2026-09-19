using ElectricalStore.Api.Http;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Ordering;
using Permixa.AspNetCore.Authorization;

namespace ElectricalStore.Api.Endpoints;

public static class OrderingSettingsEndpoints
{
    public static IEndpointRouteBuilder MapOrderingSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin/settings/ordering")
            .WithTags("Back Office - Settings")
            .RequireAuthorization();

        admin.MapGet("/", async (
                GetOrderingSettingsUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var dto = await useCase.ExecuteAsync(cancellationToken);
                return Results.Ok(dto);
            })
            .RequirePermission(AppPermissions.Settings.Manage)
            .WithName("GetOrderingSettings")
            .WithSummary("Get ordering settings (minimum merchandise subtotal)");

        admin.MapPut("/", async (
                UpdateOrderingSettingsRequest request,
                UpdateOrderingSettingsUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return result.ToHttpResult();
            })
            .RequirePermission(AppPermissions.Settings.Manage)
            .WithName("UpdateOrderingSettings")
            .WithSummary("Update ordering settings (minimum merchandise subtotal)");

        return app;
    }
}
