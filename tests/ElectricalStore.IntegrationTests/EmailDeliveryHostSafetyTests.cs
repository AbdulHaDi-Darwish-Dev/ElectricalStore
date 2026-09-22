using System.Net;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ElectricalStore.IntegrationTests;

/// <summary>
/// Production fail-closed email delivery + Development-only outbox mapping.
/// </summary>
public sealed class EmailDeliveryHostSafetyTests
{
    [Fact]
    public async Task Production_RequireConfirmedEmail_MissingResendConfig_FailsStartup()
    {
        await using var factory = new ProductionMissingEmailConfigFactory();
        await ((IAsyncLifetime)factory).InitializeAsync();

        var ex = Assert.ThrowsAny<Exception>(() =>
        {
            using var _ = factory.CreateClient();
        });

        var text = FlattenExceptionMessage(ex);
        Assert.Contains("email verification", text, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            text.Contains("Resend", StringComparison.OrdinalIgnoreCase)
            || text.Contains("FrontendPublicUrl", StringComparison.OrdinalIgnoreCase)
            || text.Contains("FromEmail", StringComparison.OrdinalIgnoreCase),
            $"Expected production email gate message, got: {text}");
    }

    [Fact]
    public async Task Production_UseCapturingSenderTrue_WithoutResend_FailsClosed()
    {
        await using var factory = new ProductionCapturingAttemptFactory();
        await ((IAsyncLifetime)factory).InitializeAsync();

        var ex = Assert.ThrowsAny<Exception>(() =>
        {
            using var _ = factory.CreateClient();
        });

        var text = FlattenExceptionMessage(ex);
        Assert.Contains("email verification", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UseCapturingSender is not allowed in Production", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_DevEmailOutbox_IsNotMapped()
    {
        await using var factory = new ProductionConfiguredEmailFactory();
        await ((IAsyncLifetime)factory).InitializeAsync();

        var client = factory.CreateClient();
        var get = await client.GetAsync("/dev/email-outbox");
        var delete = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/dev/email-outbox"));

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var preview = await client.GetAsync($"/dev/email-outbox/{Guid.NewGuid():D}/preview");
        var text = await client.GetAsync($"/dev/email-outbox/{Guid.NewGuid():D}/text");
        Assert.Equal(HttpStatusCode.NotFound, preview.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, text.StatusCode);
    }

    private static string FlattenExceptionMessage(Exception ex)
    {
        var parts = new List<string>();
        for (var cur = ex; cur is not null; cur = cur.InnerException)
            parts.Add(cur.Message);
        return string.Join(" | ", parts);
    }
}

/// <summary>Production + RequireConfirmedEmail without delivery config.</summary>
file sealed class ProductionMissingEmailConfigFactory : AppWebApplicationFactory
{
    protected override string EnvironmentName => Environments.Production;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Email:UseCapturingSender", "false");
        builder.UseSetting("Email:FrontendPublicUrl", "");
        builder.UseSetting("Email:FromEmail", "");
        builder.UseSetting("Email:Resend:ApiKey", "");
    }
}

/// <summary>Production attempting capturing sender without Resend key must fail closed.</summary>
file sealed class ProductionCapturingAttemptFactory : AppWebApplicationFactory
{
    protected override string EnvironmentName => Environments.Production;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Email:UseCapturingSender", "true");
        builder.UseSetting("Email:FrontendPublicUrl", "https://store.example.test");
        builder.UseSetting("Email:FromEmail", "noreply@example.test");
        builder.UseSetting("Email:Resend:ApiKey", "");
    }
}

/// <summary>
/// Production with Resend placeholder. Capturing flag must not expose /dev/email-outbox
/// (Development-only mapping + capturing registration blocked in Production).
/// </summary>
file sealed class ProductionConfiguredEmailFactory : AppWebApplicationFactory
{
    protected override string EnvironmentName => Environments.Production;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Email:UseCapturingSender", "true");
        builder.UseSetting("Email:FrontendPublicUrl", "https://store.example.test");
        builder.UseSetting("Email:FromEmail", "noreply@example.test");
        builder.UseSetting("Email:Resend:ApiKey", "re_test_placeholder_not_called");
    }
}
