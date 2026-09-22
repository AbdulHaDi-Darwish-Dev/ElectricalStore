using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
}

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
            TextBody = message.TextBody
        });
        return Task.CompletedTask;
    }
}

public class AppWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
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

        // UseSetting wins over empty appsettings ConnectionStrings:Default.
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
        builder.UseSetting("Permixa:Authentication:RequireConfirmedEmail", "false");
        builder.UseSetting("Media:MaxImageSizeMb", "5");
        // Keep production-shaped Login rate limit (20/min) in integration tests.
        builder.UseSetting("LocalDevFixtures:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(Images);
        });
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
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
