using Microsoft.Extensions.DependencyInjection;
using ElectricalStore.Api.Hosting;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Permixa.Infrastructure.Bootstrap;
using Permixa.Infrastructure.Persistence;

namespace ElectricalStore.Api.DependencyInjection;

/// <summary>
/// Development-only host initialization (migrate → bootstrap → optional app permission seed).
/// Production never runs these steps from this helper.
/// </summary>
public static class DevelopmentInitializationExtensions
{
    public static async Task InitializeDevelopmentAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        if (!app.Environment.IsDevelopment())
            return;

        await using var scope = app.Services.CreateAsyncScope();

        var permixaDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await permixaDb.Database.MigrateAsync(cancellationToken);

        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await appDb.Database.MigrateAsync(cancellationToken);

        if (!await appDb.OrderingSettings.AnyAsync(cancellationToken))
        {
            appDb.OrderingSettings.Add(
                ElectricalStore.Domain.Ordering.OrderingSettings.CreateDefault(0m));
            await appDb.SaveChangesAsync(cancellationToken);
        }

        await scope.ServiceProvider.GetRequiredService<IPermixaBootstrapper>()
            .BootstrapAsync(cancellationToken);

        if (app.Configuration.GetValue("Permixa:AppSeed:Enabled", false))
        {
            await scope.ServiceProvider.GetRequiredService<AppPermissionSeeder>()
                .SeedAsync(cancellationToken);
        }

        if (app.Configuration.GetValue($"{LocalDevFixtureOptions.SectionName}:Enabled", false))
        {
            await scope.ServiceProvider.GetRequiredService<LocalDevCatalogFixtureSeeder>()
                .SeedAsync(cancellationToken);
            await scope.ServiceProvider.GetRequiredService<LocalDevAccountFixtureSeeder>()
                .SeedAsync(cancellationToken);
        }
    }
}
