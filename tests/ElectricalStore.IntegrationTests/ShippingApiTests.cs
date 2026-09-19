using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Application.Authorization;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class ShippingApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public ShippingApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_Crud_Activate_Duplicate_AndPublicActiveOnly()
    {
        var owner = await CreateOwnerClientAsync();

        var me = await owner.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using (var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync()))
        {
            var permissions = doc.RootElement.GetProperty("permissions")
                .EnumerateArray()
                .Select(p => p.GetString())
                .ToHashSet(StringComparer.Ordinal);
            Assert.Contains(AppPermissions.Shipping.Manage, permissions);
        }

        var create = await owner.PostAsJsonAsync("/admin/shipping/zones", new
        {
            name = "  Azizieh  ",
            fee = 1500m,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<ZoneResponse>(Json))!;
        Assert.Equal("Azizieh", created.Name);
        Assert.Equal(1500m, created.Fee);
        Assert.True(created.IsActive);

        var zero = await owner.PostAsJsonAsync("/admin/shipping/zones", new
        {
            name = "Free Pickup " + Guid.NewGuid().ToString("N")[..6],
            fee = 0m,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, zero.StatusCode);

        var negative = await owner.PostAsJsonAsync("/admin/shipping/zones", new
        {
            name = "Bad Fee",
            fee = -1m,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);

        var duplicate = await owner.PostAsJsonAsync("/admin/shipping/zones", new
        {
            name = "azizieh",
            fee = 10m,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("Shipping.NameAlreadyExists", await duplicate.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var get = await owner.GetAsync($"/admin/shipping/zones/{created.Id}");
        get.EnsureSuccessStatusCode();

        var missing = await owner.GetAsync($"/admin/shipping/zones/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var update = await owner.PutAsJsonAsync($"/admin/shipping/zones/{created.Id}", new
        {
            name = "Azizieh Updated",
            fee = 1750.25m
        });
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<ZoneResponse>(Json))!;
        Assert.Equal("Azizieh Updated", updated.Name);
        Assert.Equal(1750.25m, updated.Fee);

        var deactivate = await owner.PostAsync($"/admin/shipping/zones/{created.Id}/deactivate", null);
        deactivate.EnsureSuccessStatusCode();

        var anonymous = _factory.CreateClient();
        var publicList = await anonymous.GetAsync("/shipping/zones");
        publicList.EnsureSuccessStatusCode();
        var zones = (await publicList.Content.ReadFromJsonAsync<List<PublicZoneResponse>>(Json))!;
        Assert.DoesNotContain(zones, z => z.Id == created.Id);

        var activate = await owner.PostAsync($"/admin/shipping/zones/{created.Id}/activate", null);
        activate.EnsureSuccessStatusCode();

        var publicAgain = await anonymous.GetAsync("/shipping/zones");
        publicAgain.EnsureSuccessStatusCode();
        var zonesAgain = (await publicAgain.Content.ReadFromJsonAsync<List<PublicZoneResponse>>(Json))!;
        Assert.Contains(zonesAgain, z => z.Id == created.Id && z.Name == "Azizieh Updated" && z.Fee == 1750.25m);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/shipping/zones")).StatusCode);

        var user = await CreateUserWithoutShippingPermissionAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/shipping/zones")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await user.PostAsJsonAsync("/admin/shipping/zones", new { name = "Nope", fee = 1m, isActive = true })).StatusCode);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private async Task<HttpClient> CreateUserWithoutShippingPermissionAsync()
    {
        var client = _factory.CreateClient();
        var email = $"noperm_ship_{Guid.NewGuid():N}@example.test";
        var userName = $"s{Guid.NewGuid():N}"[..12];

        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            userName,
            email,
            password = TestKeys.UserPassword
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private sealed class ZoneResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class PublicZoneResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
    }
}
