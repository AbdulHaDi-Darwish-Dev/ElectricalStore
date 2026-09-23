using ElectricalStore.Api.Hosting;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class DemoCatalogApiTests : IClassFixture<DemoCatalogWebApplicationFactory>
{
    private readonly DemoCatalogWebApplicationFactory _factory;

    public DemoCatalogApiTests(DemoCatalogWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_IsIdempotent_AndUsesDemoSkuPrefix_WithoutTouchingE2EFixtures()
    {
        using var scope1 = _factory.Services.CreateScope();
        var seeder = scope1.ServiceProvider.GetRequiredService<DemoCatalogSeeder>();

        var first = await seeder.SeedAsync();
        Assert.True(first.Ran);
        Assert.NotNull(first.Stats);
        Assert.True(first.Stats!.Categories >= 8);
        Assert.True(first.Stats.Products >= 35);
        Assert.True(first.Stats.Variants >= 40);

        using var scope2 = _factory.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder2 = scope2.ServiceProvider.GetRequiredService<DemoCatalogSeeder>();

        var demoSkuCountBefore = await db.ProductVariants.CountAsync(v =>
            v.Sku.StartsWith(DemoCatalogDefinitions.SkuPrefix));

        var second = await seeder2.SeedAsync();
        Assert.True(second.Ran);

        var demoSkuCountAfter = await db.ProductVariants.CountAsync(v =>
            v.Sku.StartsWith(DemoCatalogDefinitions.SkuPrefix));
        Assert.Equal(demoSkuCountBefore, demoSkuCountAfter);

        Assert.False(await db.Categories.AnyAsync(c => c.Name == "E2E Category"));
        Assert.False(await db.Products.AnyAsync(p => p.Name == "E2E Product"));
        Assert.False(await db.ProductVariants.AnyAsync(v => v.Sku == "E2E-STD-001"));

        var skus = await db.ProductVariants
            .Where(v => v.Sku.StartsWith(DemoCatalogDefinitions.SkuPrefix))
            .Select(v => v.Sku)
            .ToListAsync();
        Assert.Equal(skus.Count, skus.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var meter = await db.ProductVariants.FirstAsync(v => v.Sku == "DEMO-CBL-2P5");
        Assert.Equal(SellingUnit.Meter, meter.SellingUnit);
        Assert.True(meter.QuantityIncrement > 0);

        var piece = await db.ProductVariants.FirstAsync(v => v.Sku == "DEMO-BRK-1P-020");
        Assert.Equal(SellingUnit.Piece, piece.SellingUnit);
        Assert.Equal(1m, piece.QuantityIncrement);
    }

    [Fact]
    public async Task Seed_WhenDisabled_DoesNothing()
    {
        await using var factory = new DemoCatalogDisabledWebApplicationFactory();
        await factory.InitializeAsync();
        _ = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        // Seeder is not registered when disabled.
        Assert.Null(scope.ServiceProvider.GetService<DemoCatalogSeeder>());
    }
}

public sealed class DemoCatalogWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("DemoCatalog:Enabled", "true");
        builder.UseSetting("DemoCatalog:PublicBaseUrl", "http://localhost:5180");
        builder.UseSetting("LocalDevFixtures:Enabled", "false");
        builder.ConfigureServices(services =>
        {
            services.AddScoped<DemoCatalogSeeder>();
        });
    }
}

public sealed class DemoCatalogDisabledWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("DemoCatalog:Enabled", "false");
        builder.UseSetting("LocalDevFixtures:Enabled", "false");
    }
}
