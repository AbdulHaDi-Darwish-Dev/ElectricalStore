using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Api.Hosting;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

/// <summary>
/// Verifies ForwardedHeaders trust boundary: trusted hops rewrite RemoteIp;
/// untrusted peers cannot spoof X-Forwarded-For; distinct clients partition differently.
/// </summary>
public sealed class ForwardedHeadersApiTests :
    IClassFixture<ForwardedHeadersWebApplicationFactory>,
    IClassFixture<ForwardedHeadersDisabledWebApplicationFactory>
{
    private const string TrustedProxy = "10.10.0.2";
    private const string UntrustedPeer = "198.51.100.10";
    private const string ClientA = "203.0.113.50";
    private const string ClientB = "203.0.113.60";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ForwardedHeadersWebApplicationFactory _factory;
    private readonly ForwardedHeadersDisabledWebApplicationFactory _disabledFactory;

    public ForwardedHeadersApiTests(
        ForwardedHeadersWebApplicationFactory factory,
        ForwardedHeadersDisabledWebApplicationFactory disabledFactory)
    {
        _factory = factory;
        _disabledFactory = disabledFactory;
    }

    [Fact]
    public async Task TrustedProxy_ForwardedClientIp_BecomesRemoteIp()
    {
        var client = _factory.CreateClient();

        using var request = CreateRemoteIpRequest(TrustedProxy, ClientA);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RemoteIpDto>(Json);
        Assert.Equal(ClientA, NormalizeIp(body!.RemoteIp));
    }

    [Fact]
    public async Task SpoofedXForwardedFor_FromUntrustedPeer_IsIgnored()
    {
        var client = _factory.CreateClient();

        using var request = CreateRemoteIpRequest(UntrustedPeer, ClientA);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RemoteIpDto>(Json);
        Assert.Equal(UntrustedPeer, NormalizeIp(body!.RemoteIp));
        Assert.NotEqual(ClientA, NormalizeIp(body.RemoteIp));
    }

    [Fact]
    public async Task ForwardedHeadersDisabled_IgnoresXForwardedFor_EvenFromConfiguredProxyIp()
    {
        var client = _disabledFactory.CreateClient();

        using var request = CreateRemoteIpRequest(TrustedProxy, ClientA);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RemoteIpDto>(Json);
        Assert.Equal(TrustedProxy, NormalizeIp(body!.RemoteIp));
    }

    [Fact]
    public async Task TwoForwardedClients_ResolveToDifferentRemoteIps()
    {
        var client = _factory.CreateClient();

        using var requestA = CreateRemoteIpRequest(TrustedProxy, ClientA);
        using var requestB = CreateRemoteIpRequest(TrustedProxy, ClientB);

        var bodyA = await (await client.SendAsync(requestA)).Content.ReadFromJsonAsync<RemoteIpDto>(Json);
        var bodyB = await (await client.SendAsync(requestB)).Content.ReadFromJsonAsync<RemoteIpDto>(Json);

        Assert.Equal(ClientA, NormalizeIp(bodyA!.RemoteIp));
        Assert.Equal(ClientB, NormalizeIp(bodyB!.RemoteIp));
        Assert.NotEqual(NormalizeIp(bodyA.RemoteIp), NormalizeIp(bodyB.RemoteIp));
    }

    [Fact]
    public async Task SameForwardedClient_ResolvesToSameRemoteIp()
    {
        var client = _factory.CreateClient();

        using var first = CreateRemoteIpRequest(TrustedProxy, ClientA);
        using var second = CreateRemoteIpRequest(TrustedProxy, ClientA);

        var body1 = await (await client.SendAsync(first)).Content.ReadFromJsonAsync<RemoteIpDto>(Json);
        var body2 = await (await client.SendAsync(second)).Content.ReadFromJsonAsync<RemoteIpDto>(Json);

        Assert.Equal(NormalizeIp(body1!.RemoteIp), NormalizeIp(body2!.RemoteIp));
        Assert.Equal(ClientA, NormalizeIp(body1.RemoteIp));
    }

    [Fact]
    public async Task LoginRateLimit_PartitionsByResolvedRemoteIp_NotSharedAcrossForwardedClients()
    {
        // Permixa Login policy remains 20/min RemoteIp — do not change limits for the test.
        var client = _factory.CreateClient();

        // Exhaust ClientA partition (21 attempts → expect 429 on the last).
        HttpResponseMessage? lastA = null;
        for (var i = 0; i < 21; i++)
        {
            lastA?.Dispose();
            lastA = await SendLoginAsync(client, TrustedProxy, ClientA);
        }

        Assert.NotNull(lastA);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastA!.StatusCode);

        // ClientB behind the same trusted proxy must use a different partition.
        using var responseB = await SendLoginAsync(client, TrustedProxy, ClientB);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, responseB.StatusCode);
        // Invalid credentials → 401 (or similar auth failure), proving the request was not rate-limited.
        Assert.Equal(HttpStatusCode.Unauthorized, responseB.StatusCode);
    }

    private static HttpRequestMessage CreateRemoteIpRequest(string connectingIp, string? forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/__test/remote-ip");
        request.Headers.TryAddWithoutValidation(TestConnectingIpStartupFilter.HeaderName, connectingIp);
        if (forwardedFor is not null)
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        return request;
    }

    private static async Task<HttpResponseMessage> SendLoginAsync(
        HttpClient client,
        string connectingIp,
        string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new
            {
                emailOrUserName = $"nobody-{forwardedFor.Replace('.', '-')}@example.invalid",
                password = "not-a-real-password"
            })
        };
        request.Headers.TryAddWithoutValidation(TestConnectingIpStartupFilter.HeaderName, connectingIp);
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        return await client.SendAsync(request);
    }

    private static string NormalizeIp(string? ip)
    {
        Assert.False(string.IsNullOrWhiteSpace(ip));
        var parsed = IPAddress.Parse(ip!);
        if (parsed.IsIPv4MappedToIPv6)
            parsed = parsed.MapToIPv4();
        return parsed.ToString();
    }

    private sealed class RemoteIpDto
    {
        public string? RemoteIp { get; set; }
    }
}

/// <summary>ForwardedHeaders enabled; trusts 10.10.0.2 only; exposes RemoteIp probe.</summary>
public sealed class ForwardedHeadersWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ForwardedHeaders:Enabled", "true");
        builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.10.0.2");
        builder.UseSetting("Testing:MapRemoteIpEndpoint", "true");
        builder.UseSetting("Testing:AllowConnectingIpOverride", "true");
    }
}

/// <summary>ForwardedHeaders disabled — X-Forwarded-For must not rewrite RemoteIp.</summary>
public sealed class ForwardedHeadersDisabledWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ForwardedHeaders:Enabled", "false");
        builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.10.0.2");
        builder.UseSetting("Testing:MapRemoteIpEndpoint", "true");
        builder.UseSetting("Testing:AllowConnectingIpOverride", "true");
    }
}
