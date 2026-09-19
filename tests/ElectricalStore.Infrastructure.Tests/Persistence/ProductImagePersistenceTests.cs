using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.Infrastructure.Tests.Persistence;

public sealed class ProductImagePersistenceTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Task InitializeAsync() => _sql.StartAsync();

    public Task DisposeAsync() => _sql.DisposeAsync().AsTask();

    [Fact]
    public async Task AddProductImage_PersistsAndReloads()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sql.GetConnectionString(), sql =>
                sql.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTable))
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var category = Category.Create("Img Cat", null, true);
        category.SetImage("cat/1", "https://cdn.test/cat.jpg");
        db.Categories.Add(category);

        var product = Product.Create(
            "Img Product",
            null,
            category.Id,
            true,
            [new ProductVariantSeed("Std", "IMG-SKU-1", 10m, SellingUnit.Piece, 1m, true)]);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var tracked = await db.Products
            .Include(p => p.Images)
            .FirstAsync(p => p.Id == product.Id);
        tracked.AddImage("prod/1", "https://cdn.test/p1.jpg");
        await db.SaveChangesAsync();

        var reloaded = await db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .FirstAsync(p => p.Id == product.Id);

        Assert.Single(reloaded.Images);
        Assert.True(reloaded.Images.First().IsPrimary);
        Assert.Equal("prod/1", reloaded.Images.First().StorageKey);
    }
}
