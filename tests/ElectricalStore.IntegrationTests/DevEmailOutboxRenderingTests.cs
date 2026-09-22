using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Permixa.Infrastructure.Email;
using HostCapturingEmailSender = ElectricalStore.Api.Hosting.CapturingEmailSender;
using Xunit;

namespace ElectricalStore.IntegrationTests;

/// <summary>
/// Development outbox captures the real rendered HTML and can preview it in a browser.
/// Uses the host CapturingEmailSender (not the test-only email double).
/// </summary>
public sealed class DevEmailOutboxRenderingTests : IClassFixture<DevOutboxWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly DevOutboxWebApplicationFactory _factory;

    public DevEmailOutboxRenderingTests(DevOutboxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Outbox_CapturesArabicHtml_AndPreviewRendersExactBody()
    {
        var client = _factory.CreateClient();
        await client.DeleteAsync("/dev/email-outbox");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"outbox-html-{suffix}@example.test";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "مستخدم العرض",
            email,
            password = "Customer-Outbox-Pass-1!"
        })).EnsureSuccessStatusCode();

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/dev/email-outbox");
        listRequest.Headers.Accept.ParseAdd("application/json");
        var listResponse = await client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/json", listResponse.Content.Headers.ContentType?.MediaType);

        var messages = (await listResponse.Content.ReadFromJsonAsync<List<OutboxItem>>(Json))!;
        var message = Assert.Single(
            messages,
            m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase));

        Assert.False(string.IsNullOrWhiteSpace(message.Subject));
        Assert.Contains("تأكيد", message.Subject, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(message.TextBody));
        Assert.Contains("verify-email", message.TextBody, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(message.HtmlBody));
        Assert.Contains("<!DOCTYPE html>", message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dir=\"rtl\"", message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("تأكيد البريد الإلكتروني", message.HtmlBody, StringComparison.Ordinal);
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Contains(message.Id.ToString("D"), message.PreviewUrl, StringComparison.OrdinalIgnoreCase);

        var preview = await client.GetAsync($"/dev/email-outbox/{message.Id:D}/preview");
        preview.EnsureSuccessStatusCode();
        Assert.Equal("text/html", preview.Content.Headers.ContentType?.MediaType);
        var previewHtml = await preview.Content.ReadAsStringAsync();
        Assert.Equal(message.HtmlBody, previewHtml);

        var plain = await client.GetAsync($"/dev/email-outbox/{message.Id:D}/text");
        plain.EnsureSuccessStatusCode();
        Assert.Equal("text/plain", plain.Content.Headers.ContentType?.MediaType);
        Assert.Equal(message.TextBody, await plain.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Outbox_BrowserAccept_ReturnsHtmlIndex()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/dev/email-outbox");
        request.Headers.Accept.ParseAdd(
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Development email outbox", html, StringComparison.Ordinal);
        Assert.True(
            html.Contains("Open HTML preview", StringComparison.Ordinal)
            || html.Contains("No messages captured yet", StringComparison.Ordinal),
            "Expected either message actions or the empty-state copy.");
    }

    private sealed class OutboxItem
    {
        public Guid Id { get; set; }
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string TextBody { get; set; } = "";
        public string HtmlBody { get; set; } = "";
        public string PreviewUrl { get; set; } = "";
    }
}

/// <summary>
/// Keeps host CapturingEmailSender so /dev/email-outbox routes are mapped.
/// </summary>
public sealed class DevOutboxWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.RemoveAll<HostCapturingEmailSender>();
            services.AddSingleton<HostCapturingEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<HostCapturingEmailSender>());
        });
    }
}
