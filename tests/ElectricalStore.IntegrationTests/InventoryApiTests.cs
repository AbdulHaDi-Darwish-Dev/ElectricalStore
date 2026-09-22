using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Application.Authorization;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class InventoryApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public InventoryApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Adjust_List_Get_Audit_Permissions_AndPublicAvailability()
    {
        var ownerClient = _factory.CreateClient();
        var tokens = await ownerClient.LoginAsOwnerAsync();
        var owner = _factory.CreateAuthenticatedClient(tokens.AccessToken);
        var (productId, variantId) = await CreateCatalogReadyProductAsync(owner);

        var anonymous = _factory.CreateClient();
        var beforeStock = await anonymous.GetAsync($"/catalog/products/{productId}");
        beforeStock.EnsureSuccessStatusCode();
        var detailBefore = (await beforeStock.Content.ReadFromJsonAsync<CatalogDetail>(Json))!;
        Assert.Single(detailBefore.Variants);
        Assert.Equal(0m, detailBefore.Variants[0].AvailableQuantity);
        Assert.False(detailBefore.Variants[0].IsInStock);

        var stillListed = await anonymous.GetAsync("/catalog/products");
        stillListed.EnsureSuccessStatusCode();
        var list = (await stillListed.Content.ReadFromJsonAsync<List<CatalogListItem>>(Json))!;
        Assert.Contains(list, p => p.Id == productId);

        var missing = await owner.PostAsJsonAsync(
            $"/admin/inventory/{Guid.NewGuid()}/adjust",
            new { quantityDelta = 1m, reason = "nope" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var zero = await owner.PostAsJsonAsync(
            $"/admin/inventory/{variantId}/adjust",
            new { quantityDelta = 0m, reason = "noop" });
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);

        var adjust = await owner.PostAsJsonAsync(
            $"/admin/inventory/{variantId}/adjust",
            new { quantityDelta = 12.5m, reason = "Initial stock" });
        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);
        var adjusted = (await adjust.Content.ReadFromJsonAsync<InventoryRow>(Json))!;
        Assert.Equal(12.5m, adjusted.OnHand);
        Assert.Equal(0m, adjusted.Reserved);
        Assert.Equal(12.5m, adjusted.Available);
        Assert.True(adjusted.IsInStock);

        var get = await owner.GetAsync($"/admin/inventory/{variantId}");
        get.EnsureSuccessStatusCode();
        var got = (await get.Content.ReadFromJsonAsync<InventoryRow>(Json))!;
        Assert.Equal(variantId, got.VariantId);
        Assert.Equal(12.5m, got.Available);

        var listAdmin = await owner.GetAsync($"/admin/inventory?search={Uri.EscapeDataString(got.Sku)}");
        listAdmin.EnsureSuccessStatusCode();
        var rows = (await listAdmin.Content.ReadFromJsonAsync<List<InventoryRow>>(Json))!;
        Assert.Contains(rows, r => r.VariantId == variantId && r.OnHand == 12.5m);

        var inStock = await owner.GetAsync("/admin/inventory?inStock=true");
        inStock.EnsureSuccessStatusCode();
        Assert.Contains(
            (await inStock.Content.ReadFromJsonAsync<List<InventoryRow>>(Json))!,
            r => r.VariantId == variantId);

        var adjustments = await owner.GetAsync($"/admin/inventory/{variantId}/adjustments");
        adjustments.EnsureSuccessStatusCode();
        var history = (await adjustments.Content.ReadFromJsonAsync<List<AdjustmentRow>>(Json))!;
        Assert.Single(history);
        Assert.Equal(12.5m, history[0].QuantityDelta);
        Assert.Equal(tokens.UserId, history[0].PerformedByUserId);

        var afterStock = await anonymous.GetAsync($"/catalog/products/{productId}");
        afterStock.EnsureSuccessStatusCode();
        var detailAfter = (await afterStock.Content.ReadFromJsonAsync<CatalogDetail>(Json))!;
        Assert.Equal(12.5m, detailAfter.Variants[0].AvailableQuantity);
        Assert.True(detailAfter.Variants[0].IsInStock);

        var me = await owner.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var permissions = doc.RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Select(p => p.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(AppPermissions.Inventory.Read, permissions);
        Assert.Contains(AppPermissions.Inventory.Adjust, permissions);

        var userClient = await CreateUserWithoutInventoryPermissionAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.GetAsync("/admin/inventory")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await userClient.PostAsJsonAsync(
                $"/admin/inventory/{variantId}/adjust",
                new { quantityDelta = 1m, reason = "no" })).StatusCode);

        var unauth = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await unauth.GetAsync("/admin/inventory")).StatusCode);
    }

    [Fact]
    public async Task Adjust_BelowReserved_ReturnsConflict()
    {
        var owner = await CreateOwnerClientAsync();
        var (_, variantId) = await CreateCatalogReadyProductAsync(owner);

        var seed = await owner.PostAsJsonAsync(
            $"/admin/inventory/{variantId}/adjust",
            new { quantityDelta = 10m, reason = "seed" });
        seed.EnsureSuccessStatusCode();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var item = await db.InventoryItems.FirstAsync(x => x.ProductVariantId == variantId);
            item.Reserve(4m);
            await db.SaveChangesAsync();
        }

        var bad = await owner.PostAsJsonAsync(
            $"/admin/inventory/{variantId}/adjust",
            new { quantityDelta = -7m, reason = "too much" });
        Assert.Equal(HttpStatusCode.Conflict, bad.StatusCode);
        Assert.Contains("Inventory.OnHandBelowReserved", await bad.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var ok = await owner.PostAsJsonAsync(
            $"/admin/inventory/{variantId}/adjust",
            new { quantityDelta = -6m, reason = "down to reserved" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var row = (await ok.Content.ReadFromJsonAsync<InventoryRow>(Json))!;
        Assert.Equal(4m, row.OnHand);
        Assert.Equal(4m, row.Reserved);
        Assert.Equal(0m, row.Available);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private async Task<HttpClient> CreateUserWithoutInventoryPermissionAsync()
    {
        var client = _factory.CreateClient();
        var email = $"noperm_inv_{Guid.NewGuid():N}@example.test";
        var userName = $"i{Guid.NewGuid():N}"[..12];

        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            userName,
            email,
            password = TestKeys.UserPassword
        });
        register.EnsureSuccessStatusCode();

        await _factory.MarkEmailConfirmedAsync(email);

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private async Task<(Guid ProductId, Guid VariantId)> CreateCatalogReadyProductAsync(HttpClient owner)
    {
        var category = await CreateCategoryAsync(owner, "Inv Cat " + Guid.NewGuid().ToString("N")[..6], true);
        await UploadAsync(owner, HttpMethod.Put, $"/admin/categories/{category.Id}/image");

        var sku = "INV-" + Guid.NewGuid().ToString("N")[..8];
        var create = await owner.PostAsJsonAsync("/admin/products", new
        {
            name = "Inv Product " + sku,
            description = "stock",
            categoryId = category.Id,
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Standard",
                    sku,
                    price = 100m,
                    sellingUnit = "Meter",
                    quantityIncrement = 0.5m,
                    isActive = true
                }
            }
        });
        create.EnsureSuccessStatusCode();
        var product = (await create.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        await UploadAsync(owner, HttpMethod.Post, $"/admin/products/{product.Id}/images");
        return (product.Id, product.Variants[0].Id);
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient owner, string name, bool isActive)
    {
        var response = await owner.PostAsJsonAsync("/admin/categories", new { name, isActive });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
    }

    private static async Task UploadAsync(HttpClient owner, HttpMethod method, string url)
    {
        byte[] jpeg =
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
        ];
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "img.jpg");
        using var request = new HttpRequestMessage(method, url) { Content = content };
        var response = await owner.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private sealed class CategoryResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class ProductResponse
    {
        public Guid Id { get; set; }
        public List<VariantResponse> Variants { get; set; } = new();
    }

    private sealed class VariantResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public bool IsInStock { get; set; }
    }

    private sealed class CatalogListItem
    {
        public Guid Id { get; set; }
    }

    private sealed class CatalogDetail
    {
        public Guid Id { get; set; }
        public List<VariantResponse> Variants { get; set; } = new();
    }

    private sealed class InventoryRow
    {
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal OnHand { get; set; }
        public decimal Reserved { get; set; }
        public decimal Available { get; set; }
        public bool IsInStock { get; set; }
    }

    private sealed class AdjustmentRow
    {
        public decimal QuantityDelta { get; set; }
        public Guid PerformedByUserId { get; set; }
    }
}
