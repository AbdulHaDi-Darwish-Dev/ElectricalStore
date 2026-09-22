using Microsoft.Extensions.DependencyInjection;
using ElectricalStore.Api.Hosting;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Permixa.Application.Verification.Abstractions;
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

        await EnsureBootstrapOwnerEmailConfirmedAsync(scope.ServiceProvider, app.Configuration, cancellationToken);

        if (app.Configuration.GetValue("Permixa:AppSeed:Enabled", false))
        {
            await scope.ServiceProvider.GetRequiredService<AppPermissionSeeder>()
                .SeedAsync(cancellationToken);
        }

        if (app.Configuration.GetValue($"{LocalDevFixtureOptions.SectionName}:Enabled", false))
        {
            try
            {
                await scope.ServiceProvider.GetRequiredService<LocalDevCatalogFixtureSeeder>()
                    .SeedAsync(cancellationToken);
                await scope.ServiceProvider.GetRequiredService<LocalDevAccountFixtureSeeder>()
                    .SeedAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("LocalDevFixtures");
                logger.LogError(ex, "LocalDevFixtures seeding failed; host continues for Development.");
            }
        }
    }

    /// <summary>
    /// Permixa RequireConfirmedEmail applies to all logins. Bootstrap Owner must be able to sign in.
    /// </summary>
    private static async Task EnsureBootstrapOwnerEmailConfirmedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var ownerEmail = configuration["Permixa:Bootstrap:OwnerEmail"];
        if (string.IsNullOrWhiteSpace(ownerEmail))
            return;

        var emails = services.GetRequiredService<IIdentityUserEmailReader>();
        var confirm = services.GetRequiredService<IIdentityEmailConfirmation>();
        var ownerId = await emails.FindUserIdByEmailAsync(ownerEmail.Trim(), cancellationToken);
        if (ownerId is null)
            return;

        if (await emails.IsEmailConfirmedAsync(ownerId.Value, cancellationToken))
            return;

        await confirm.MarkEmailConfirmedAsync(ownerId.Value, cancellationToken);
    }
}
