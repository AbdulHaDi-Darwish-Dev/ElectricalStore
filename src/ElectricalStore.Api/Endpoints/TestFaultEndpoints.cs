namespace ElectricalStore.Api.Endpoints;

/// <summary>
/// Test-only fault endpoint. Disabled unless Testing:MapFaultEndpoint=true
/// (IntegrationTests). Proves unexpected exceptions still map to 500 InternalError.
/// </summary>
public static class TestFaultEndpoints
{
    public static IEndpointRouteBuilder MapTestFaultEndpointIfEnabled(this IEndpointRouteBuilder endpoints)
    {
        var enabled = endpoints.ServiceProvider
            .GetRequiredService<IConfiguration>()
            .GetValue("Testing:MapFaultEndpoint", false);

        if (!enabled)
            return endpoints;

        endpoints.MapPost("/__test/fault", () =>
        {
            throw new InvalidOperationException("Controlled test fault.");
        });

        return endpoints;
    }
}
