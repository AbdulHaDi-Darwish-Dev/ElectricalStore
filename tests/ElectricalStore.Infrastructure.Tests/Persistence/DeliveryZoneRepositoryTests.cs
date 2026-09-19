using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Shipping;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace ElectricalStore.Infrastructure.Tests.Persistence;

public sealed class DeliveryZoneRepositoryTests : IAsyncLifetime
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
    public async Task AddAndList_RoundTrips_AndActiveFilter()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var repo = new DeliveryZoneRepository(db);
        var uow = new AppUnitOfWork(db);

        await repo.AddAsync(DeliveryZone.Create("Active Zone", 1000m, true));
        await repo.AddAsync(DeliveryZone.Create("Inactive Zone", 500m, false));
        await uow.SaveChangesAsync();

        var all = await repo.ListAsync(activeOnly: false);
        var active = await repo.ListAsync(activeOnly: true);

        Assert.Equal(2, all.Count);
        Assert.Single(active);
        Assert.Equal("Active Zone", active[0].Name);
    }

    [Fact]
    public async Task UniqueNormalizedName_RejectsCaseInsensitiveDuplicates()
    {
        var options = CreateOptions();
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();

        var repo = new DeliveryZoneRepository(db);
        var uow = new AppUnitOfWork(db);

        await repo.AddAsync(DeliveryZone.Create("Azizieh", 100m, true));
        await uow.SaveChangesAsync();

        await repo.AddAsync(DeliveryZone.Create("azizieh", 200m, true));
        await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => uow.SaveChangesAsync());
    }
}
