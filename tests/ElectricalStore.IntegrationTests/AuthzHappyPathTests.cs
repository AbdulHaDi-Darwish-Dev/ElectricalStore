using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Permixa.Domain.Authorization;
using Permixa.Infrastructure.Persistence;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class AuthzHappyPathTests : IClassFixture<AppWebApplicationFactory>
{
    private readonly AppWebApplicationFactory _factory;

    public AuthzHappyPathTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Bootstrap_Seed_Login_ProtectedEndpoint_Succeeds_WithoutManualCacheClear()
    {
        var client = _factory.CreateClient();
        var tokens = await client.LoginAsOwnerAsync();
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));

        var authed = _factory.CreateAuthenticatedClient(tokens.AccessToken);
        var protectedResponse = await authed.GetAsync("/admin/categories");
        protectedResponse.EnsureSuccessStatusCode();

        var me = await authed.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        var permissions = doc.RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Select(p => p.GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Categories.Manage", permissions);
        Assert.Contains("Orders.Manage", permissions);
        Assert.Contains("Iam.Users.Read", permissions);

        await using var scope = _factory.Services.CreateAsyncScope();
        var state = await scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>()
            .Set<AuthorizationState>()
            .AsNoTracking()
            .SingleAsync();
        // Bootstrap grants IAM catalog (increments) + app seed increments when grants are added.
        Assert.True(state.RbacVersion > AuthorizationState.InitialRbacVersion);
    }

    [Fact]
    public async Task BaseAuthFlow_Register_Login_Refresh_Me_Protected_Logout()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            userName = "alice",
            email = "alice@example.test",
            password = TestKeys.UserPassword
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = "alice@example.test",
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;

        var refresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = tokens.RefreshToken });
        refresh.EnsureSuccessStatusCode();
        var refreshed = (await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;

        var aliceClient = _factory.CreateAuthenticatedClient(refreshed.AccessToken);
        (await aliceClient.GetAsync("/me")).EnsureSuccessStatusCode();

        // Unprivileged registered user is denied; Owner (seeded grants) succeeds.
        Assert.Equal(
            System.Net.HttpStatusCode.Forbidden,
            (await aliceClient.GetAsync("/admin/categories")).StatusCode);

        var owner = await client.LoginAsOwnerAsync();
        var ownerClient = _factory.CreateAuthenticatedClient(owner.AccessToken);
        (await ownerClient.GetAsync("/admin/categories")).EnsureSuccessStatusCode();

        var logout = await client.PostAsJsonAsync("/auth/logout", new { refreshToken = refreshed.RefreshToken });
        logout.EnsureSuccessStatusCode();
    }
}

public sealed class MigrationIsolationTests : IClassFixture<AppWebApplicationFactory>
{
    private readonly AppWebApplicationFactory _factory;

    public MigrationIsolationTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SharedDatabase_UsesSeparateMigrationHistoryTables()
    {
        _ = _factory.CreateClient(); // ensure Development startup migrate ran

        await using var scope = _factory.Services.CreateAsyncScope();
        var permixaDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connection = permixaDb.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME;
            """;
        var tables = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
        }

        Assert.Contains("__EFMigrationsHistory", tables);
        Assert.Contains("__AppMigrationsHistory", tables);
        Assert.DoesNotContain("SampleNotes", tables);
        Assert.Contains("Categories", tables);
        Assert.Contains("Products", tables);
        Assert.Contains("ProductVariants", tables);
        Assert.Contains("ProductImages", tables);
        Assert.Contains("InventoryItems", tables);
        Assert.Contains("InventoryAdjustments", tables);
        Assert.Contains("DeliveryZones", tables);
        Assert.Contains("AspNetUsers", tables);
        Assert.Contains("Permissions", tables);

        await using var hist = connection.CreateCommand();
        hist.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
        var permixaMigrations = Convert.ToInt32(await hist.ExecuteScalarAsync());
        Assert.True(permixaMigrations >= 1);

        hist.CommandText = "SELECT COUNT(*) FROM __AppMigrationsHistory;";
        var appMigrations = Convert.ToInt32(await hist.ExecuteScalarAsync());
        Assert.Equal(11, appMigrations);

        Assert.Contains("Orders", tables);
        Assert.Contains("OrderItems", tables);
        Assert.Contains("OrderModificationAudits", tables);
        Assert.Contains("OrderingSettings", tables);
        Assert.Contains("OrderPlacementIdempotencies", tables);

        Assert.Same(
            permixaDb.Database.GetConnectionString(),
            appDb.Database.GetConnectionString());
    }
}

public sealed class EnvironmentBehaviorTests
{
    [Fact]
    public async Task Production_DoesNotAutoMigrateBootstrapOrSeed()
    {
        await using var factory = new ProductionNoMigrateFactory();
        await ((IAsyncLifetime)factory).InitializeAsync();
        try
        {
            using var client = factory.CreateClient();
            // Host starts; Development-only startup block skipped — no schema created.
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME IN ('__EFMigrationsHistory', '__AppMigrationsHistory', 'AspNetUsers', 'Categories');
                """;
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(0, count);
        }
        finally
        {
            await ((IAsyncLifetime)factory).DisposeAsync();
        }
    }
}

/// <summary>Production environment — startup migrate/bootstrap/seed must not run.</summary>
file sealed class ProductionNoMigrateFactory : AppWebApplicationFactory
{
    protected override string EnvironmentName => Environments.Production;
}
