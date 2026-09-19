using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class ApiErrorHandlingTests : IClassFixture<ApiErrorHandlingWebApplicationFactory>
{
    private readonly ApiErrorHandlingWebApplicationFactory _factory;

    public ApiErrorHandlingTests(ApiErrorHandlingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MalformedBooleanLiteral_Returns400_InvalidRequest()
    {
        var auth = await CreateOwnerClientAsync();
        var body = """{"name":"Test","description":"","isActive": fals}""";
        var response = await PostRawAsync(auth, "/admin/categories", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertInvalidRequestProblemAsync(response);
    }

    [Fact]
    public async Task SyntacticallyBrokenJson_Returns400_InvalidRequest()
    {
        var auth = await CreateOwnerClientAsync();
        var body = """{"name":"Test","description":"","isActive": false""";
        var response = await PostRawAsync(auth, "/admin/categories", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertInvalidRequestProblemAsync(response);
    }

    [Fact]
    public async Task WrongJsonTypeForProperty_Returns400_InvalidRequest()
    {
        var auth = await CreateOwnerClientAsync();
        var body = """{"name":"Test","description":"","isActive":"not-a-boolean"}""";
        var response = await PostRawAsync(auth, "/admin/categories", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertInvalidRequestProblemAsync(response);
    }

    [Fact]
    public async Task EmptyCategoryName_Returns400_CategoryNameRequired()
    {
        var auth = await CreateOwnerClientAsync();
        var response = await auth.PostAsJsonAsync("/admin/categories", new
        {
            name = "   ",
            description = "",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadAsStringAsync();
        Assert.Contains("Category.NameRequired", problem, StringComparison.Ordinal);
        Assert.DoesNotContain("InternalError", problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DuplicateCategoryName_Returns409_NameAlreadyExists()
    {
        var auth = await CreateOwnerClientAsync();
        var name = "Dup " + Guid.NewGuid().ToString("N")[..8];

        var first = await auth.PostAsJsonAsync("/admin/categories", new { name, isActive = true });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await auth.PostAsJsonAsync("/admin/categories", new { name, isActive = true });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("Category.NameAlreadyExists", problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingCategory_Returns404_NotFound()
    {
        var auth = await CreateOwnerClientAsync();
        var response = await auth.GetAsync($"/admin/categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadAsStringAsync();
        Assert.Contains("Category.NotFound", problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedException_Returns500_InternalError()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/__test/fault", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadAsStringAsync();
        Assert.Contains("InternalError", problem, StringComparison.Ordinal);
        Assert.Contains("An unexpected error occurred.", problem, StringComparison.Ordinal);
        Assert.DoesNotContain("Controlled test fault", problem, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidRequest", problem, StringComparison.Ordinal);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private static async Task<HttpResponseMessage> PostRawAsync(HttpClient client, string url, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    private static async Task AssertInvalidRequestProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("Bad Request", root.GetProperty("title").GetString());
        Assert.Equal("InvalidRequest", root.GetProperty("code").GetString());
        Assert.Equal(
            "The request body is invalid or could not be parsed.",
            root.GetProperty("detail").GetString());
        Assert.False(root.TryGetProperty("exception", out _));
        Assert.False(root.TryGetProperty("stackTrace", out _));
    }
}

/// <summary>Enables the controlled test fault endpoint for unexpected-exception coverage.</summary>
public sealed class ApiErrorHandlingWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Testing:MapFaultEndpoint", "true");
    }
}
