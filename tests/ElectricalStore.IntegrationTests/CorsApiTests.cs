using System.Net;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

/// <summary>
/// Focused CORS contract checks for the approved Next.js origin (http://localhost:3000).
/// Relies on Development <c>Cors:AllowedOrigins</c> via <see cref="AppWebApplicationFactory"/>.
/// </summary>
public sealed class CorsApiTests : IClassFixture<AppWebApplicationFactory>
{
    private const string AllowedOrigin = "http://localhost:3000";
    private const string DisallowedOrigin = "http://evil.example";

    private readonly AppWebApplicationFactory _factory;

    public CorsApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_Succeeds_WithExpectedCorsHeaders()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/catalog/products");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add(
            "Access-Control-Request-Headers",
            "authorization,content-type,idempotency-key,x-order-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, GetHeader(response, "Access-Control-Allow-Origin"));

        var allowMethods = GetHeader(response, "Access-Control-Allow-Methods") ?? string.Empty;
        Assert.Contains("GET", allowMethods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST", allowMethods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PUT", allowMethods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE", allowMethods, StringComparison.OrdinalIgnoreCase);

        var allowHeaders = GetHeader(response, "Access-Control-Allow-Headers") ?? string.Empty;
        Assert.Contains("Authorization", allowHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content-Type", allowHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Idempotency-Key", allowHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("X-Order-Token", allowHeaders, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            response.Headers,
            h => h.Key.Equals("Access-Control-Allow-Credentials", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AllowedOrigin_Get_Receives_AccessControlAllowOrigin()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AllowedOrigin, GetHeader(response, "Access-Control-Allow-Origin"));

        var exposed = GetHeader(response, "Access-Control-Expose-Headers") ?? string.Empty;
        Assert.Contains("Retry-After", exposed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisallowedOrigin_DoesNotReceive_AccessControlAllowOrigin()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(GetHeader(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task AllowedOrigin_Unauthorized_StillReceives_CorsHeaders()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/me");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(AllowedOrigin, GetHeader(response, "Access-Control-Allow-Origin"));
    }

    private static string? GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return values.FirstOrDefault();

        if (response.Content.Headers.TryGetValues(name, out var contentValues))
            return contentValues.FirstOrDefault();

        return null;
    }
}
