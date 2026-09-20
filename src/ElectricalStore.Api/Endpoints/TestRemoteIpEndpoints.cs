namespace ElectricalStore.Api.Endpoints;

/// <summary>
/// Test-only RemoteIp probe. Disabled unless Testing:MapRemoteIpEndpoint=true.
/// </summary>
public static class TestRemoteIpEndpoints
{
    public static IEndpointRouteBuilder MapTestRemoteIpEndpointIfEnabled(
        this IEndpointRouteBuilder endpoints)
    {
        var enabled = endpoints.ServiceProvider
            .GetRequiredService<IConfiguration>()
            .GetValue("Testing:MapRemoteIpEndpoint", false);

        if (!enabled)
            return endpoints;

        endpoints.MapGet("/__test/remote-ip", (HttpContext http) =>
        {
            var ip = http.Connection.RemoteIpAddress;
            return Results.Json(new
            {
                remoteIp = ip?.ToString(),
                isIPv4MappedToIPv6 = ip?.IsIPv4MappedToIPv6 ?? false
            });
        });

        return endpoints;
    }
}
