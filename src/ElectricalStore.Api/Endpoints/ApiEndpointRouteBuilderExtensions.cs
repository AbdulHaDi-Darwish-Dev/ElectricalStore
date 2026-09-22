namespace ElectricalStore.Api.Endpoints;

/// <summary>Aggregates host endpoint mapping only — no business logic.</summary>
public static class ApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithTags("Health")
            .WithName("Health")
            .WithSummary("Liveness probe");
        endpoints.MapAuthEndpoints();
        endpoints.MapMeEndpoint();
        endpoints.MapAccountEndpoints();
        endpoints.MapCategoryEndpoints();
        endpoints.MapProductEndpoints();
        endpoints.MapInventoryEndpoints();
        endpoints.MapShippingEndpoints();
        endpoints.MapOrderingEndpoints();
        endpoints.MapOrderingSettingsEndpoints();
        endpoints.MapAccessManagementEndpoints();
        endpoints.MapTestFaultEndpointIfEnabled();
        endpoints.MapTestRemoteIpEndpointIfEnabled();
        return endpoints;
    }
}
