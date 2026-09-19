using Microsoft.EntityFrameworkCore;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.Infrastructure.Persistence.Repositories;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.Infrastructure.Tests.Persistence;

public sealed class CategoryRepositoryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Task InitializeAsync() => _sql.StartAsync();

    public Task DisposeAsync() => _sql.DisposeAsync().AsTask();

    private DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sql.GetConnectionString(), sql =>
                sql.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTable))
            .Options;

    [Fact]
    public async Task AddAndGetById_RoundTrips()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var repo = new CategoryRepository(db);
        var uow = new AppUnitOfWork(db);
        var category = Category.Create("Cables", "Wire", true);
        await repo.AddAsync(category);
        await uow.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(category.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Cables", loaded!.Name);
        Assert.Equal("CABLES", loaded.NormalizedName);
        Assert.Equal("Wire", loaded.Description);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task UniqueNormalizedName_RejectsCaseInsensitiveDuplicates()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var repo = new CategoryRepository(db);
        var uow = new AppUnitOfWork(db);

        await repo.AddAsync(Category.Create("Cables", null, true));
        await uow.SaveChangesAsync();

        await repo.AddAsync(Category.Create("cables", null, true));
        var ex = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => uow.SaveChangesAsync());
        Assert.NotNull(ex.InnerException);
    }
}
