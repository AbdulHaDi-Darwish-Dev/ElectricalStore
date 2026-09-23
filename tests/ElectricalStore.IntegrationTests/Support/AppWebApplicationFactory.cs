using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ElectricalStore.Api.Hosting;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Infrastructure.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Permixa.Infrastructure.Email;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.IntegrationTests.Support;

public sealed class CapturedEmail
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string TextBody { get; init; }
    public string HtmlBody { get; init; } = "";
}

/// <summary>
/// Test double implementing Permixa IEmailSender; mirrors Api CapturingEmailSender shape.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly List<CapturedEmail> _sent = new();

    public IReadOnlyList<CapturedEmail> Sent => _sent;

    public void Clear() => _sent.Clear();

    public Task SendAsync(EmailOutgoingMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Add(new CapturedEmail
        {
            To = message.To,
            Subject = message.Subject,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody
        });
        return Task.CompletedTask;
    }
}

public class AppWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly Regex VerificationLinkRegex = new(
        @"https?://[^\s]+/verify-email\?challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
        RegexOptions.Compiled);

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public CapturingEmailSender Emails { get; } = new();

    public FakeImageStorage Images { get; } = new();

    protected virtual string EnvironmentName => Environments.Development;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.UseSetting("ConnectionStrings:Default", _sql.GetConnectionString());
        builder.UseSetting("Permixa:Jwt:Issuer", TestKeys.Issuer);
        builder.UseSetting("Permixa:Jwt:Audience", TestKeys.Audience);
        builder.UseSetting("Permixa:Jwt:PrivateKeyPem", TestKeys.PrivateKeyPem);
        builder.UseSetting("Permixa:Jwt:PublicKeyPem", TestKeys.PublicKeyPem);
        builder.UseSetting("Permixa:Bootstrap:Enabled", "true");
        builder.UseSetting("Permixa:Bootstrap:OwnerEmail", TestKeys.OwnerEmail);
        builder.UseSetting("Permixa:Bootstrap:OwnerUserName", TestKeys.OwnerUserName);
        builder.UseSetting("Permixa:Bootstrap:OwnerPassword", TestKeys.OwnerPassword);
        builder.UseSetting("Permixa:AppSeed:Enabled", "true");
        builder.UseSetting("Permixa:Authentication:RequireConfirmedEmail", "true");
        builder.UseSetting("Media:MaxImageSizeMb", "5");
        builder.UseSetting("LocalDevFixtures:Enabled", "false");
        builder.UseSetting("DemoCatalog:Enabled", "false");
        builder.UseSetting("Email:UseCapturingSender", "true");
        builder.UseSetting("Email:FrontendPublicUrl", "http://localhost:3100");
        builder.UseSetting("Email:FromEmail", "noreply@electricalstore.test");
        builder.UseSetting("Email:FromName", "ElectricalStore Test");
        builder.UseSetting("Email:Branding:ApplicationName", "ElectricalStore");
        builder.UseSetting("Email:ResendCooldownSeconds", "1");
        // Allow EmailVerificationResend rate-limit tests to isolate RemoteIp partitions.
        builder.UseSetting("Testing:AllowConnectingIpOverride", "true");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(Images);

            services.RemoveAll<IEmailSender>();
            services.RemoveAll<ElectricalStore.Api.Hosting.CapturingEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public async Task ConfirmEmailFromOutboxAsync(HttpClient client, string email)
    {
        var message = Emails.Sent.LastOrDefault(e =>
            string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(message);

        var match = VerificationLinkRegex.Match(message!.TextBody);
        Assert.True(match.Success, "Verification link missing from email text body.");

        var challengeId = Guid.Parse(match.Groups[1].Value);
        var token = Uri.UnescapeDataString(match.Groups[2].Value);

        var confirm = await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token
        });
        confirm.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Marks Identity EmailConfirmed for users created via /auth/register in tests (non-customer flows).
    /// </summary>
    public async Task MarkEmailConfirmedAsync(string email)
    {
        using var scope = Services.CreateScope();
        var emails = scope.ServiceProvider
            .GetRequiredService<Permixa.Application.Verification.Abstractions.IIdentityUserEmailReader>();
        var confirm = scope.ServiceProvider
            .GetRequiredService<Permixa.Application.Verification.Abstractions.IIdentityEmailConfirmation>();

        var userId = await emails.FindUserIdByEmailAsync(email);
        Assert.NotNull(userId);
        await confirm.MarkEmailConfirmedAsync(userId!.Value);
    }
}

public sealed class AuthTokenResponse
{
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public static class AuthClientExtensions
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<AuthTokenResponse> LoginAsOwnerAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = TestKeys.OwnerEmail,
            password = TestKeys.OwnerPassword
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
    }
}
