using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Application.Authorization;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class CategoryApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public CategoryApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicList_WorksAnonymously_AndHidesInactiveOrImageless()
    {
        var owner = await CreateOwnerClientAsync();
        var active = await CreateCategoryAsync(owner, "Public Active", isActive: true);
        var inactive = await CreateCategoryAsync(owner, "Public Hidden", isActive: false);
        await UploadCategoryImageAsync(owner, active.Id);

        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/catalog/categories");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>(Json);
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == active.Id);
        Assert.DoesNotContain(list!, c => c.Id == inactive.Id);

        Assert.Equal(HttpStatusCode.NotFound,
            (await anonymous.GetAsync($"/catalog/categories/{inactive.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await anonymous.GetAsync($"/catalog/categories/{active.Id}")).StatusCode);
    }

    [Fact]
    public async Task Admin_Create_Update_Activate_Deactivate_And_DuplicateConflict()
    {
        var owner = await CreateOwnerClientAsync();

        var create = await owner.PostAsJsonAsync("/admin/categories", new
        {
            name = "Admin Cables",
            description = "Wire products",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;

        var me = await owner.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using (var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync()))
        {
            var permissions = doc.RootElement.GetProperty("permissions")
                .EnumerateArray()
                .Select(p => p.GetString())
                .ToHashSet(StringComparer.Ordinal);
            Assert.Contains(AppPermissions.Categories.Manage, permissions);
        }

        var list = await owner.GetAsync("/admin/categories");
        list.EnsureSuccessStatusCode();

        var update = await owner.PutAsJsonAsync($"/admin/categories/{created.Id}", new
        {
            name = "Admin Cables Updated",
            description = "Updated"
        });
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
        Assert.Equal("Admin Cables Updated", updated.Name);

        var duplicate = await owner.PostAsJsonAsync("/admin/categories", new
        {
            name = "admin cables updated",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("Category.NameAlreadyExists", problem, StringComparison.Ordinal);

        var deactivate = await owner.PostAsync($"/admin/categories/{created.Id}/deactivate", null);
        deactivate.EnsureSuccessStatusCode();
        var deactivated = (await deactivate.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
        Assert.False(deactivated.IsActive);

        Assert.Equal(HttpStatusCode.NotFound,
            (await _factory.CreateClient().GetAsync($"/catalog/categories/{created.Id}")).StatusCode);

        await UploadCategoryImageAsync(owner, created.Id);

        var activate = await owner.PostAsync($"/admin/categories/{created.Id}/activate", null);
        activate.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK,
            (await _factory.CreateClient().GetAsync($"/catalog/categories/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task AdminCreate_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/admin/categories", new
        {
            name = "Unauthorized Category",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCreate_AuthenticatedWithoutPermission_Returns403()
    {
        var client = _factory.CreateClient();
        var email = $"noperm_{Guid.NewGuid():N}@example.test";
        var userName = $"u{Guid.NewGuid():N}"[..12];

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

        var response = await userClient.PostAsJsonAsync("/admin/categories", new
        {
            name = "Forbidden Category",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(
        HttpClient owner,
        string name,
        bool isActive)
    {
        var response = await owner.PostAsJsonAsync("/admin/categories", new
        {
            name,
            isActive
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(Json))!;
    }

    private static async Task UploadCategoryImageAsync(HttpClient owner, Guid categoryId)
    {
        byte[] jpeg =
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
        ];
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "cat.jpg");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/admin/categories/{categoryId}/image")
        {
            Content = content
        };
        var response = await owner.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private sealed class CategoryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
        public bool HasImage { get; set; }
    }
}
