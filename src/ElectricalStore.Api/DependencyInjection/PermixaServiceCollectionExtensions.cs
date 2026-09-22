using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ElectricalStore.Api.Hosting;
using ElectricalStore.Application.Abstractions;
using Permixa.AspNetCore.Authentication;
using Permixa.AspNetCore.Authorization;
using Permixa.AspNetCore.ProblemDetails;
using Permixa.AspNetCore.RateLimiting;
using Permixa.Infrastructure;

namespace ElectricalStore.Api.DependencyInjection;

/// <summary>
/// Host-local composition of existing Permixa NuGet registration APIs.
/// Not a Permixa framework API — lives in the generated application only.
/// </summary>
public static class PermixaServiceCollectionExtensions
{
    public static IServiceCollection AddPermixaHost(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionString)
    {
        services.AddPermixaInfrastructure(o =>
        {
            o.ConnectionString = connectionString;
            o.Bootstrap.Enabled = configuration.GetValue("Permixa:Bootstrap:Enabled", false);
            o.Bootstrap.OwnerEmail = configuration["Permixa:Bootstrap:OwnerEmail"];
            o.Bootstrap.OwnerUserName = configuration["Permixa:Bootstrap:OwnerUserName"];
            o.Bootstrap.OwnerPassword = configuration["Permixa:Bootstrap:OwnerPassword"];
        });

        services.AddPermixaAuthentication(o =>
        {
            o.Authentication.RequireConfirmedEmail =
                configuration.GetValue("Permixa:Authentication:RequireConfirmedEmail", false);
            o.Jwt.Issuer = RequireConfig(configuration, "Permixa:Jwt:Issuer");
            o.Jwt.Audience = RequireConfig(configuration, "Permixa:Jwt:Audience");
            o.Jwt.PrivateKeyPem = RequireConfig(configuration, "Permixa:Jwt:PrivateKeyPem");
        });

        services.AddPermixaAuthorization();

        // Required whenever AddPermixaAuthorization is used: admin email-change / force-password-reset
        // use cases depend on verification services. Email *delivery* (Resend) remains optional below.
        services.AddPermixaVerification();
        // Without email delivery, still register a dispatcher so Development DI validation succeeds.
        services.AddSingleton<Permixa.Application.Verification.Abstractions.IVerificationDispatcher,
            UnconfiguredVerificationDispatcher>();



        services.AddPermixaJwtBearer(o =>
        {
            o.Issuer = RequireConfig(configuration, "Permixa:Jwt:Issuer");
            o.Audience = RequireConfig(configuration, "Permixa:Jwt:Audience");
            o.PublicKeyPem = RequireConfig(configuration, "Permixa:Jwt:PublicKeyPem");
        });

        services.AddPermixaPermissionAuthorization();
        services.AddPermixaProblemDetails();

        services.AddPermixaRateLimiting(o =>
        {
            o.AddSlidingWindow("Login", p =>
            {
                // Local E2E performs many BFF logins from one loopback RemoteIp.
                // Widen only when LocalDevFixtures is enabled (never in Production compose).
                var fixturesEnabled = configuration.GetValue(
                    $"{LocalDevFixtureOptions.SectionName}:Enabled", false);
                p.PermitLimit = LocalDevLoginRateLimits.ResolvePermitLimit(
                    environment.IsDevelopment(),
                    fixturesEnabled);
                p.Window = TimeSpan.FromMinutes(1);
                p.Partition = PermixaRateLimitPartitionKind.RemoteIp;
            });
        });

        services.Configure<LocalDevFixtureOptions>(
            configuration.GetSection(LocalDevFixtureOptions.SectionName));

        if (environment.IsDevelopment()
            && configuration.GetValue("Permixa:AppSeed:Enabled", false))
        {
            // Privileged Development initialization only — not a general admin service.
            services.AddScoped<AppPermissionSeeder>();
        }

        if (environment.IsDevelopment()
            && configuration.GetValue($"{LocalDevFixtureOptions.SectionName}:Enabled", false))
        {
            services.AddScoped<LocalDevCatalogFixtureSeeder>();
            services.AddScoped<LocalDevAccountFixtureSeeder>();
        }

        services.AddScoped<ICustomerIdentityLookup, PermixaCustomerIdentityLookup>();
        services.AddScoped<ICustomerIdentityCompensation, PermixaCustomerIdentityCompensation>();
        services.AddScoped<RegisterCustomerOrchestrator>();

        return services;
    }

    private static string RequireConfig(IConfiguration configuration, string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"Missing configuration '{key}'. Use User Secrets / environment / scripts/init-dev-secrets.");
}
