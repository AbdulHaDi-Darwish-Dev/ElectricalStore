using System.Net;
using System.Text.Json;
using ElectricalStore.IntegrationTests.Support;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class OperationalLoggingTests : IClassFixture<ApiErrorHandlingWebApplicationFactory>
{
    private readonly ApiErrorHandlingWebApplicationFactory _factory;

    public OperationalLoggingTests(ApiErrorHandlingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProblemDetails_IncludeTraceId_ForClientAndServerErrors()
    {
        var client = _factory.CreateClient();
        var fault = await client.PostAsync("/__test/fault", content: null);
        Assert.Equal(HttpStatusCode.InternalServerError, fault.StatusCode);
        using (var doc = JsonDocument.Parse(await fault.Content.ReadAsStringAsync()))
        {
            Assert.True(doc.RootElement.TryGetProperty("traceId", out var traceId));
            Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        }

        var auth = _factory.CreateAuthenticatedClient((await client.LoginAsOwnerAsync()).AccessToken);
        using var content = new StringContent("""{"name":"x","isActive": fals}""", System.Text.Encoding.UTF8, "application/json");
        var bad = await auth.PostAsync("/admin/categories", content);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        using (var doc = JsonDocument.Parse(await bad.Content.ReadAsStringAsync()))
        {
            Assert.True(doc.RootElement.TryGetProperty("traceId", out var traceId));
            Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        }
    }

    [Fact]
    public async Task UnexpectedException_Returns500_WithoutLeakingExceptionMessage()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/__test/fault", content: null);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("InternalError", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Controlled test fault", body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequestLoggingMiddleware_DoesNotReferenceSensitiveHeaderNamesInSource()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "ElectricalStore.Api", "Logging", "RequestLoggingMiddleware.cs"));
        Assert.True(File.Exists(path), path);
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("Authorization", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Idempotency-Key", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Order-Token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Request.Body", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableBuffering", source, StringComparison.Ordinal);
    }
}

public sealed class ProductionEfLoggingTests : IClassFixture<ProductionEfOptionsFactory>
{
    private readonly ProductionEfOptionsFactory _factory;

    public ProductionEfLoggingTests(ProductionEfOptionsFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Production_DoesNotEnableEfSensitiveDataLogging()
    {
        _ = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        var core = options.FindExtension<CoreOptionsExtension>();
        Assert.NotNull(core);
        Assert.False(core!.IsSensitiveDataLoggingEnabled);
    }
}

/// <summary>Production environment — no migrate/bootstrap; used for EF options assertions.</summary>
public sealed class ProductionEfOptionsFactory : AppWebApplicationFactory
{
    protected override string EnvironmentName => Environments.Production;
}
