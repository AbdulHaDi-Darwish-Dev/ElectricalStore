using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Application.Authorization;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class ProductApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public ProductApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminCreate_PersistsAtomically_AndPublicCatalogRespectsVisibility()
    {
        var owner = await CreateOwnerClientAsync();
        var category = await CreateCategoryAsync(owner, "Product Cat Active", isActive: true);
        await UploadCategoryImageAsync(owner, category.Id);
        var inactiveCategory = await CreateCategoryAsync(owner, "Product Cat Hidden", isActive: false);

        var sku = "SKU-" + Guid.NewGuid().ToString("N")[..8];
        var create = await owner.PostAsJsonAsync("/admin/products", new
        {
            name = "Copper Cable",
            description = "2.5mm",
            categoryId = category.Id,
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Standard",
                    sku,
                    price = 1500m,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                },
                new
                {
                    name = "Bulk Meter",
                    sku = sku + "-M",
                    price = 900m,
                    sellingUnit = "Meter",
                    quantityIncrement = 0.5m,
                    isActive = false
                }
            }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        Assert.Equal(2, created.Variants.Count);

        var duplicate = await owner.PostAsJsonAsync("/admin/products", new
        {
            name = "Other",
            categoryId = category.Id,
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Std",
                    sku,
                    price = 1m,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                }
            }
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("Product.SkuAlreadyExists", await duplicate.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var anonymous = _factory.CreateClient();
        var listBeforeImage = await anonymous.GetAsync("/catalog/products");
        listBeforeImage.EnsureSuccessStatusCode();
        var catalogBefore = (await listBeforeImage.Content.ReadFromJsonAsync<List<CatalogListItem>>(Json))!;
        Assert.DoesNotContain(catalogBefore, p => p.Id == created.Id);

        await UploadProductImageAsync(owner, created.Id);

        var list = await anonymous.GetAsync("/catalog/products");
        list.EnsureSuccessStatusCode();
        var catalog = (await list.Content.ReadFromJsonAsync<List<CatalogListItem>>(Json))!;
        Assert.Contains(catalog, p => p.Id == created.Id);
        Assert.Equal(1500m, catalog.Single(p => p.Id == created.Id).FromPrice);
        Assert.False(string.IsNullOrWhiteSpace(catalog.Single(p => p.Id == created.Id).PrimaryImageUrl));

        var details = await anonymous.GetAsync($"/catalog/products/{created.Id}");
        details.EnsureSuccessStatusCode();
        var detail = (await details.Content.ReadFromJsonAsync<CatalogDetail>(Json))!;
        Assert.Single(detail.Variants);
        Assert.Equal("Standard", detail.Variants[0].Name);
        Assert.NotEmpty(detail.Images);

        var hiddenCreate = await owner.PostAsJsonAsync("/admin/products", new
        {
            name = "Hidden Product",
            categoryId = inactiveCategory.Id,
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Std",
                    sku = "HID-" + Guid.NewGuid().ToString("N")[..8],
                    price = 10m,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                }
            }
        });
        Assert.Equal(HttpStatusCode.Created, hiddenCreate.StatusCode);
        var hidden = (await hiddenCreate.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/catalog/products/{hidden.Id}")).StatusCode);

        var deactivate = await owner.PostAsync($"/admin/products/{created.Id}/deactivate", null);
        deactivate.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/catalog/products/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task AdminCreate_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/products", new
        {
            name = "X",
            categoryId = Guid.NewGuid(),
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Std",
                    sku = "U-" + Guid.NewGuid().ToString("N")[..8],
                    price = 1m,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                }
            }
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreate_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _factory.CreateClient();
        var email = $"noperm_p_{Guid.NewGuid():N}@example.test";
        var userName = $"p{Guid.NewGuid():N}"[..12];

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
        var userClient = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        var response = await userClient.PostAsJsonAsync("/admin/products", new
        {
            name = "Forbidden",
            categoryId = Guid.NewGuid(),
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Std",
                    sku = "F-" + Guid.NewGuid().ToString("N")[..8],
                    price = 1m,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                }
            }
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_HasProductsManagePermission()
    {
        var owner = await CreateOwnerClientAsync();
        var me = await owner.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var permissions = doc.RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Select(p => p.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(AppPermissions.Products.Manage, permissions);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient owner, string name, bool isActive)
    {
        var response = await owner.PostAsJsonAsync("/admin/categories", new { name, isActive });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
    }

    private static async Task UploadCategoryImageAsync(HttpClient owner, Guid categoryId)
    {
        await UploadAsync(owner, HttpMethod.Put, $"/admin/categories/{categoryId}/image");
    }

    private static async Task UploadProductImageAsync(HttpClient owner, Guid productId)
    {
        await UploadAsync(owner, HttpMethod.Post, $"/admin/products/{productId}/images");
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
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ProductResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<VariantResponse> Variants { get; set; } = new();
    }

    private sealed class VariantResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
    }

    private sealed class CatalogListItem
    {
        public Guid Id { get; set; }
        public decimal FromPrice { get; set; }
        public string? PrimaryImageUrl { get; set; }
    }

    private sealed class CatalogDetail
    {
        public Guid Id { get; set; }
        public List<VariantResponse> Variants { get; set; } = new();
        public List<ImageResponse> Images { get; set; } = new();
    }

    private sealed class ImageResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }
}
