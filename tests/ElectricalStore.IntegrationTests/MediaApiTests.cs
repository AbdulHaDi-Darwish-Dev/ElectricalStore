using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Application.Authorization;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class MediaApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly byte[] MinimalJpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
    ];

    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82
    ];

    private readonly AppWebApplicationFactory _factory;

    public MediaApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CategoryWithoutImage_ExcludedFromPublicCatalog_WithImageBecomesVisible()
    {
        var owner = await CreateOwnerClientAsync();
        var category = await CreateCategoryAsync(owner, "Media Cat " + Guid.NewGuid().ToString("N")[..6], true);

        var anonymous = _factory.CreateClient();
        var listBefore = (await (await anonymous.GetAsync("/catalog/categories")).Content
            .ReadFromJsonAsync<List<CategoryResponse>>(Json))!;
        Assert.DoesNotContain(listBefore, c => c.Id == category.Id);
        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/catalog/categories/{category.Id}")).StatusCode);

        var upload = await UploadPutAsync(owner, $"/admin/categories/{category.Id}/image", MinimalJpeg, "image/jpeg", "cat.jpg");
        upload.EnsureSuccessStatusCode();
        var withImage = (await upload.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
        Assert.True(withImage.HasImage);
        Assert.False(string.IsNullOrWhiteSpace(withImage.ImageUrl));

        var listAfter = (await (await anonymous.GetAsync("/catalog/categories")).Content
            .ReadFromJsonAsync<List<CategoryResponse>>(Json))!;
        Assert.Contains(listAfter, c => c.Id == category.Id && c.ImageUrl == withImage.ImageUrl);
        Assert.Equal(HttpStatusCode.OK,
            (await anonymous.GetAsync($"/catalog/categories/{category.Id}")).StatusCode);

        var delete = await owner.DeleteAsync($"/admin/categories/{category.Id}/image");
        delete.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/catalog/categories/{category.Id}")).StatusCode);
    }

    [Fact]
    public async Task ProductImages_PrimaryCapVisibilityAndAuth()
    {
        _factory.Images.FailNextUpload = false;
        var owner = await CreateOwnerClientAsync();
        var category = await CreateCategoryAsync(owner, "Prod Media Cat " + Guid.NewGuid().ToString("N")[..6], true);
        (await UploadPutAsync(owner, $"/admin/categories/{category.Id}/image", MinimalPng, "image/png", "c.png"))
            .EnsureSuccessStatusCode();

        var product = await CreateProductAsync(owner, category.Id, "Media Product");
        var anonymous = _factory.CreateClient();
        Assert.DoesNotContain(
            (await (await anonymous.GetAsync("/catalog/products")).Content
                .ReadFromJsonAsync<List<CatalogListItem>>(Json))!,
            p => p.Id == product.Id);

        var first = await UploadPostAsync(owner, $"/admin/products/{product.Id}/images", MinimalJpeg, "image/jpeg", "1.jpg");
        first.EnsureSuccessStatusCode();
        var afterFirst = (await first.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        Assert.Single(afterFirst.Images);
        Assert.True(afterFirst.Images[0].IsPrimary);

        for (var i = 2; i <= 4; i++)
        {
            var next = await UploadPostAsync(owner, $"/admin/products/{product.Id}/images", MinimalJpeg, "image/jpeg", $"{i}.jpg");
            next.EnsureSuccessStatusCode();
        }

        var fifth = await UploadPostAsync(owner, $"/admin/products/{product.Id}/images", MinimalJpeg, "image/jpeg", "5.jpg");
        Assert.Equal(HttpStatusCode.Conflict, fifth.StatusCode);
        Assert.Contains("Product.TooManyImages", await fifth.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var admin = (await (await owner.GetAsync($"/admin/products/{product.Id}")).Content
            .ReadFromJsonAsync<ProductResponse>(Json))!;
        Assert.Equal(4, admin.Images.Count);
        var nonPrimary = admin.Images.First(i => !i.IsPrimary);
        var setPrimary = await owner.PostAsync(
            $"/admin/products/{product.Id}/images/{nonPrimary.Id}/primary", null);
        setPrimary.EnsureSuccessStatusCode();
        var afterPrimary = (await setPrimary.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        Assert.Equal(nonPrimary.Id, afterPrimary.Images.Single(i => i.IsPrimary).Id);

        var catalog = (await (await anonymous.GetAsync("/catalog/products")).Content
            .ReadFromJsonAsync<List<CatalogListItem>>(Json))!;
        var listed = Assert.Single(catalog, p => p.Id == product.Id);
        Assert.False(string.IsNullOrWhiteSpace(listed.PrimaryImageUrl));

        var detail = (await (await anonymous.GetAsync($"/catalog/products/{product.Id}")).Content
            .ReadFromJsonAsync<CatalogDetail>(Json))!;
        Assert.Equal(4, detail.Images.Count);
        Assert.Equal(1, detail.Images.Count(i => i.IsPrimary));
        Assert.True(detail.Images.SequenceEqual(detail.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)));

        var primaryId = afterPrimary.Images.Single(i => i.IsPrimary).Id;
        var deletePrimary = await owner.DeleteAsync($"/admin/products/{product.Id}/images/{primaryId}");
        deletePrimary.EnsureSuccessStatusCode();
        var afterDeletePrimary = (await deletePrimary.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
        Assert.Equal(3, afterDeletePrimary.Images.Count);
        Assert.Single(afterDeletePrimary.Images, i => i.IsPrimary);

        foreach (var image in afterDeletePrimary.Images.ToList())
        {
            (await owner.DeleteAsync($"/admin/products/{product.Id}/images/{image.Id}")).EnsureSuccessStatusCode();
        }

        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/catalog/products/{product.Id}")).StatusCode);
        Assert.DoesNotContain(
            (await (await anonymous.GetAsync("/catalog/products")).Content
                .ReadFromJsonAsync<List<CatalogListItem>>(Json))!,
            p => p.Id == product.Id);
    }

    [Fact]
    public async Task Upload_RejectsInvalidTypeAndOversized_AndUnauthorized()
    {
        var owner = await CreateOwnerClientAsync();
        var category = await CreateCategoryAsync(owner, "Val Cat " + Guid.NewGuid().ToString("N")[..6], true);

        var invalidType = await UploadPutAsync(
            owner,
            $"/admin/categories/{category.Id}/image",
            "not-an-image"u8.ToArray(),
            "text/plain",
            "x.txt");
        Assert.Equal(HttpStatusCode.BadRequest, invalidType.StatusCode);
        Assert.Contains("Media.InvalidContentType", await invalidType.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var fakeJpegHeaderButWrong = await UploadPutAsync(
            owner,
            $"/admin/categories/{category.Id}/image",
            "notjpegcontent"u8.ToArray(),
            "image/jpeg",
            "x.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, fakeJpegHeaderButWrong.StatusCode);
        Assert.Contains("Media.InvalidImageContent", await fakeJpegHeaderButWrong.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var huge = new byte[6 * 1024 * 1024];
        MinimalJpeg.CopyTo(huge, 0);
        var tooLarge = await UploadPutAsync(
            owner,
            $"/admin/categories/{category.Id}/image",
            huge,
            "image/jpeg",
            "big.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Contains("Media.FileTooLarge", await tooLarge.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var anonymous = _factory.CreateClient();
        var unauth = await UploadPutAsync(
            anonymous,
            $"/admin/categories/{category.Id}/image",
            MinimalJpeg,
            "image/jpeg",
            "cat.jpg");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
    }

    [Fact]
    public async Task CloudinaryFailure_Returns503_WithoutLeakingSecrets_AndCompensatesOnDbFailurePath()
    {
        var owner = await CreateOwnerClientAsync();
        var category = await CreateCategoryAsync(owner, "Fail Cat " + Guid.NewGuid().ToString("N")[..6], true);

        _factory.Images.FailNextUpload = true;
        var failed = await UploadPutAsync(
            owner,
            $"/admin/categories/{category.Id}/image",
            MinimalJpeg,
            "image/jpeg",
            "cat.jpg");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        var body = await failed.Content.ReadAsStringAsync();
        Assert.Contains("Media.UploadFailed", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiSecret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cloudinary", body, StringComparison.OrdinalIgnoreCase);

        // Compensation: successful upload then forced storage delete count when replacing/deleting.
        (await UploadPutAsync(owner, $"/admin/categories/{category.Id}/image", MinimalJpeg, "image/jpeg", "ok.jpg"))
            .EnsureSuccessStatusCode();
        var deletesBefore = _factory.Images.DeleteCount;
        (await owner.DeleteAsync($"/admin/categories/{category.Id}/image")).EnsureSuccessStatusCode();
        Assert.True(_factory.Images.DeleteCount > deletesBefore);
    }

    [Fact]
    public async Task ProductImageUpload_WithoutPermission_Returns403()
    {
        var client = _factory.CreateClient();
        var email = $"noperm_m_{Guid.NewGuid():N}@example.test";
        var userName = $"m{Guid.NewGuid():N}"[..12];
        (await client.PostAsJsonAsync("/auth/register", new
        {
            userName,
            email,
            password = TestKeys.UserPassword
        })).EnsureSuccessStatusCode();
        await _factory.MarkEmailConfirmedAsync(email);
        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var userClient = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        var response = await UploadPostAsync(
            userClient,
            $"/admin/products/{Guid.NewGuid()}/images",
            MinimalJpeg,
            "image/jpeg",
            "x.jpg");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_StillHasManagePermissionsForMedia()
    {
        var owner = await CreateOwnerClientAsync();
        using var doc = JsonDocument.Parse(await (await owner.GetAsync("/me")).Content.ReadAsStringAsync());
        var permissions = doc.RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Select(p => p.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(AppPermissions.Categories.Manage, permissions);
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

    private static async Task<ProductResponse> CreateProductAsync(HttpClient owner, Guid categoryId, string name)
    {
        var sku = "IMG-" + Guid.NewGuid().ToString("N")[..8];
        var response = await owner.PostAsJsonAsync("/admin/products", new
        {
            name,
            categoryId,
            isActive = true,
            variants = new[]
            {
                new
                {
                    name = "Std",
                    sku,
                    price = 100m,
                    sellingUnit = "Piece",
                    quantityIncrement = 1m,
                    isActive = true
                }
            }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(Json))!;
    }

    private static Task<HttpResponseMessage> UploadPutAsync(
        HttpClient client,
        string url,
        byte[] bytes,
        string contentType,
        string fileName) =>
        UploadAsync(client, HttpMethod.Put, url, bytes, contentType, fileName);

    private static Task<HttpResponseMessage> UploadPostAsync(
        HttpClient client,
        string url,
        byte[] bytes,
        string contentType,
        string fileName) =>
        UploadAsync(client, HttpMethod.Post, url, bytes, contentType, fileName);

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        byte[] bytes,
        string contentType,
        string fileName)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        return await client.SendAsync(request);
    }

    private sealed class CategoryResponse
    {
        public Guid Id { get; set; }
        public string? ImageUrl { get; set; }
        public bool HasImage { get; set; }
    }

    private sealed class ProductResponse
    {
        public Guid Id { get; set; }
        public List<ImageResponse> Images { get; set; } = new();
    }

    private sealed class ImageResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    private sealed class CatalogListItem
    {
        public Guid Id { get; set; }
        public string PrimaryImageUrl { get; set; } = string.Empty;
    }

    private sealed class CatalogDetail
    {
        public Guid Id { get; set; }
        public List<ImageResponse> Images { get; set; } = new();
    }
}
