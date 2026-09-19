using System.Net;
using System.Text.Json;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

/// <summary>
/// Low-cost OpenAPI organization/security regression checks (documentation metadata only).
/// </summary>
public sealed class OpenApiOrganizationTests : IClassFixture<AppWebApplicationFactory>
{
    private readonly AppWebApplicationFactory _factory;

    public OpenApiOrganizationTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Swagger_Categories_UseFeatureTags_AndCorrectBearerMetadata()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("components", out var components));
        Assert.True(components.TryGetProperty("securitySchemes", out var schemes));
        Assert.True(schemes.TryGetProperty("Bearer", out _));

        var paths = root.GetProperty("paths");

        AssertPathTag(paths, "/auth/login", "post", "Authentication");
        AssertPathTag(paths, "/auth/register", "post", "Authentication");
        AssertNoSecurity(paths, "/auth/login", "post");
        AssertPathTag(paths, "/me", "get", "Account");
        AssertHasBearerSecurity(paths, "/me", "get");

        AssertPathTag(paths, "/catalog/categories", "get", "Catalog - Categories");
        AssertPathTag(paths, "/catalog/categories/{id}", "get", "Catalog - Categories");
        AssertNoSecurity(paths, "/catalog/categories", "get");
        AssertNoSecurity(paths, "/catalog/categories/{id}", "get");

        AssertPathTag(paths, "/admin/categories", "get", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories", "post", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories/{id}", "get", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories/{id}", "put", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories/{id}/activate", "post", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories/{id}/deactivate", "post", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories/{id}/image", "put", "Back Office - Categories");
        AssertPathTag(paths, "/admin/categories/{id}/image", "delete", "Back Office - Categories");

        AssertHasBearerSecurity(paths, "/admin/categories", "get");
        AssertHasBearerSecurity(paths, "/admin/categories", "post");
        AssertHasBearerSecurity(paths, "/admin/categories/{id}", "get");
        AssertHasBearerSecurity(paths, "/admin/categories/{id}", "put");
        AssertHasBearerSecurity(paths, "/admin/categories/{id}/activate", "post");
        AssertHasBearerSecurity(paths, "/admin/categories/{id}/deactivate", "post");
        AssertHasBearerSecurity(paths, "/admin/categories/{id}/image", "put");
        AssertHasBearerSecurity(paths, "/admin/categories/{id}/image", "delete");

        Assert.Equal("List active categories", GetSummary(paths, "/catalog/categories", "get"));
        Assert.Equal("Create category", GetSummary(paths, "/admin/categories", "post"));
        Assert.Equal("ListActiveCategories", GetOperationId(paths, "/catalog/categories", "get"));
        Assert.Equal("CreateCategory", GetOperationId(paths, "/admin/categories", "post"));

        AssertPathTag(paths, "/catalog/products", "get", "Catalog - Products");
        AssertPathTag(paths, "/catalog/products/{id}", "get", "Catalog - Products");
        AssertNoSecurity(paths, "/catalog/products", "get");
        AssertHasBearerSecurity(paths, "/admin/products", "post");
        AssertPathTag(paths, "/admin/products", "post", "Back Office - Products");
        AssertPathTag(paths, "/admin/products/{id}/images", "post", "Back Office - Products");
        AssertPathTag(paths, "/admin/products/{id}/images/{imageId}", "delete", "Back Office - Products");
        AssertPathTag(paths, "/admin/products/{id}/images/{imageId}/primary", "post", "Back Office - Products");
        AssertHasBearerSecurity(paths, "/admin/products/{id}/images", "post");
        Assert.Equal("ListCatalogProducts", GetOperationId(paths, "/catalog/products", "get"));
        Assert.Equal("CreateProduct", GetOperationId(paths, "/admin/products", "post"));

        AssertPathTag(paths, "/admin/inventory", "get", "Back Office - Inventory");
        AssertPathTag(paths, "/admin/inventory/{variantId}", "get", "Back Office - Inventory");
        AssertPathTag(paths, "/admin/inventory/{variantId}/adjust", "post", "Back Office - Inventory");
        AssertPathTag(paths, "/admin/inventory/{variantId}/adjustments", "get", "Back Office - Inventory");
        AssertHasBearerSecurity(paths, "/admin/inventory", "get");
        AssertHasBearerSecurity(paths, "/admin/inventory/{variantId}/adjust", "post");
        Assert.Equal("ListAdminInventory", GetOperationId(paths, "/admin/inventory", "get"));
        Assert.Equal("AdjustInventory", GetOperationId(paths, "/admin/inventory/{variantId}/adjust", "post"));

        AssertPathTag(paths, "/shipping/zones", "get", "Shipping");
        AssertNoSecurity(paths, "/shipping/zones", "get");
        AssertPathTag(paths, "/admin/shipping/zones", "get", "Back Office - Shipping");
        AssertPathTag(paths, "/admin/shipping/zones", "post", "Back Office - Shipping");
        AssertHasBearerSecurity(paths, "/admin/shipping/zones", "get");
        AssertHasBearerSecurity(paths, "/admin/shipping/zones", "post");
        Assert.Equal("ListActiveDeliveryZones", GetOperationId(paths, "/shipping/zones", "get"));
        Assert.Equal("CreateDeliveryZone", GetOperationId(paths, "/admin/shipping/zones", "post"));

        AssertPathTag(paths, "/checkout/preview", "post", "Checkout");
        AssertNoSecurity(paths, "/checkout/preview", "post");
        Assert.Equal("CheckoutPreview", GetOperationId(paths, "/checkout/preview", "post"));

        AssertPathTag(paths, "/orders", "post", "Orders");
        AssertPathTag(paths, "/orders", "get", "Orders");
        AssertPathTag(paths, "/orders/{id}", "get", "Orders");
        AssertPathTag(paths, "/orders/{id}/track", "get", "Orders");
        AssertHasBearerSecurity(paths, "/orders", "get");
        AssertHasBearerSecurity(paths, "/orders/{id}", "get");
        AssertNoSecurity(paths, "/orders/{id}/track", "get");

        AssertPathTag(paths, "/admin/orders", "get", "Back Office - Orders");
        AssertPathTag(paths, "/admin/orders/{id}/confirm", "post", "Back Office - Orders");
        AssertPathTag(paths, "/admin/orders/{id}/prepare", "post", "Back Office - Orders");
        AssertPathTag(paths, "/admin/orders/{id}/out-for-delivery", "post", "Back Office - Orders");
        AssertPathTag(paths, "/admin/orders/{id}/deliver", "post", "Back Office - Orders");
        AssertPathTag(paths, "/admin/orders/{id}/cancel", "post", "Back Office - Orders");
        AssertPathTag(paths, "/admin/orders/{id}/mark-paid", "post", "Back Office - Orders");
        AssertHasBearerSecurity(paths, "/admin/orders", "get");
        AssertHasBearerSecurity(paths, "/admin/orders/{id}/confirm", "post");
        Assert.Equal("ListAdminOrders", GetOperationId(paths, "/admin/orders", "get"));
        Assert.Equal("ConfirmOrder", GetOperationId(paths, "/admin/orders/{id}/confirm", "post"));

        AssertPathTag(paths, "/admin/settings/ordering", "get", "Back Office - Settings");
        AssertPathTag(paths, "/admin/settings/ordering", "put", "Back Office - Settings");
        AssertHasBearerSecurity(paths, "/admin/settings/ordering", "get");
        AssertHasBearerSecurity(paths, "/admin/settings/ordering", "put");
        Assert.Equal("GetOrderingSettings", GetOperationId(paths, "/admin/settings/ordering", "get"));

        AssertPathTag(paths, "/admin/access/users", "get", "Back Office - Access Management");
        AssertPathTag(paths, "/admin/access/users/{id}", "get", "Back Office - Access Management");
        AssertPathTag(paths, "/admin/access/users/{id}/roles", "put", "Back Office - Access Management");
        AssertPathTag(paths, "/admin/access/users/{id}/permissions", "get", "Back Office - Access Management");
        AssertPathTag(paths, "/admin/access/roles", "get", "Back Office - Access Management");
        AssertPathTag(paths, "/admin/access/roles", "post", "Back Office - Access Management");
        AssertPathTag(paths, "/admin/access/permissions", "get", "Back Office - Access Management");
        AssertHasBearerSecurity(paths, "/admin/access/users", "get");
        AssertHasBearerSecurity(paths, "/admin/access/roles", "post");
        AssertHasBearerSecurity(paths, "/admin/access/permissions", "get");
        Assert.Equal("ListAccessUsers", GetOperationId(paths, "/admin/access/users", "get"));
        Assert.Equal("CreateAccessRole", GetOperationId(paths, "/admin/access/roles", "post"));
        Assert.Equal("ListAccessPermissions", GetOperationId(paths, "/admin/access/permissions", "get"));

        var trackOp = paths.GetProperty("/orders/{id}/track").GetProperty("get");
        Assert.True(trackOp.TryGetProperty("parameters", out var trackParams));
        Assert.Contains(trackParams.EnumerateArray(), p =>
            p.TryGetProperty("name", out var n) && n.GetString() == "X-Order-Token");

        var placeOp = paths.GetProperty("/orders").GetProperty("post");
        Assert.True(placeOp.TryGetProperty("parameters", out var placeParams));
        Assert.Contains(placeParams.EnumerateArray(), p =>
            p.TryGetProperty("name", out var n) && n.GetString() == "Idempotency-Key");
    }

    private static void AssertPathTag(JsonElement paths, string path, string method, string expectedTag)
    {
        var op = paths.GetProperty(path).GetProperty(method);
        var tags = op.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray();
        Assert.Contains(expectedTag, tags);
    }

    private static void AssertNoSecurity(JsonElement paths, string path, string method)
    {
        var op = paths.GetProperty(path).GetProperty(method);
        if (!op.TryGetProperty("security", out var security))
            return;
        Assert.Equal(0, security.GetArrayLength());
    }

    private static void AssertHasBearerSecurity(JsonElement paths, string path, string method)
    {
        var op = paths.GetProperty(path).GetProperty(method);
        Assert.True(op.TryGetProperty("security", out var security));
        Assert.True(security.GetArrayLength() > 0);
        var foundBearer = security.EnumerateArray().Any(req =>
            req.TryGetProperty("Bearer", out _));
        Assert.True(foundBearer, $"Expected Bearer security on {method.ToUpperInvariant()} {path}");
    }

    private static string? GetSummary(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("summary").GetString();

    private static string? GetOperationId(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("operationId").GetString();
}
