using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ElectricalStore.Api.Hosting;
using ElectricalStore.Application.Abstractions;
using Permixa.AspNetCore.Authentication;
using Permixa.AspNetCore.Authorization;
using Permixa.AspNetCore.ProblemDetails;
using Permixa.AspNetCore.RateLimiting;
using Permixa.Email.Resend;
using Permixa.Infrastructure;
using Permixa.Infrastructure.Email;
using Permixa.Application.Verification.Abstractions;

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

        var requireConfirmedEmail = configuration.GetValue(
            "Permixa:Authentication:RequireConfirmedEmail",
            false);

        services.AddPermixaAuthentication(o =>
        {
            o.Authentication.RequireConfirmedEmail = requireConfirmedEmail;
            o.Jwt.Issuer = RequireConfig(configuration, "Permixa:Jwt:Issuer");
            o.Jwt.Audience = RequireConfig(configuration, "Permixa:Jwt:Audience");
            o.Jwt.PrivateKeyPem = RequireConfig(configuration, "Permixa:Jwt:PrivateKeyPem");
        });

        services.AddPermixaAuthorization();

        var email = configuration.GetSection(EmailDeliveryHostOptions.SectionName)
            .Get<EmailDeliveryHostOptions>() ?? new EmailDeliveryHostOptions();

        services.Configure<EmailDeliveryHostOptions>(
            configuration.GetSection(EmailDeliveryHostOptions.SectionName));

        services.AddPermixaVerification(o =>
        {
            if (email.UrlTokenLifetimeMinutes is > 0)
                o.Verification.UrlTokenLifetime = TimeSpan.FromMinutes(email.UrlTokenLifetimeMinutes.Value);
            if (email.ResendCooldownSeconds is > 0)
                o.Verification.ResendCooldown = TimeSpan.FromSeconds(email.ResendCooldownSeconds.Value);
        });

        RegisterEmailDelivery(services, environment, requireConfirmedEmail, email);

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
                var fixturesEnabled = configuration.GetValue(
                    $"{LocalDevFixtureOptions.SectionName}:Enabled", false);
                p.PermitLimit = LocalDevLoginRateLimits.ResolvePermitLimit(
                    environment.IsDevelopment(),
                    fixturesEnabled);
                p.Window = TimeSpan.FromMinutes(1);
                p.Partition = PermixaRateLimitPartitionKind.RemoteIp;
            });

            // Public resend is anti-enumeration; still bound by IP to limit mail/provider abuse.
            o.AddSlidingWindow("EmailVerificationResend", p =>
            {
                p.PermitLimit = 5;
                p.Window = TimeSpan.FromMinutes(15);
                p.Partition = PermixaRateLimitPartitionKind.RemoteIp;
            });

            // Public forgot-password is anti-enumeration; still bound by IP.
            // Development + LocalDevFixtures widens like Login so Playwright recovery journeys
            // do not trip the production-class 5/15min budget on loopback.
            o.AddSlidingWindow("PasswordForgot", p =>
            {
                var fixturesEnabled = configuration.GetValue(
                    $"{LocalDevFixtureOptions.SectionName}:Enabled", false);
                p.PermitLimit = environment.IsDevelopment() && fixturesEnabled ? 100 : 5;
                p.Window = TimeSpan.FromMinutes(15);
                p.Partition = PermixaRateLimitPartitionKind.RemoteIp;
            });

            // Authenticated email-change request; partition by user when available.
            o.AddSlidingWindow("EmailChangeRequest", p =>
            {
                var fixturesEnabled = configuration.GetValue(
                    $"{LocalDevFixtureOptions.SectionName}:Enabled", false);
                p.PermitLimit = environment.IsDevelopment() && fixturesEnabled ? 40 : 5;
                p.Window = TimeSpan.FromMinutes(15);
                p.Partition = PermixaRateLimitPartitionKind.AuthenticatedUserId;
            });
        });

        services.Configure<LocalDevFixtureOptions>(
            configuration.GetSection(LocalDevFixtureOptions.SectionName));
        services.Configure<DemoCatalogOptions>(
            configuration.GetSection(DemoCatalogOptions.SectionName));

        if (environment.IsDevelopment()
            && configuration.GetValue("Permixa:AppSeed:Enabled", false))
        {
            services.AddScoped<AppPermissionSeeder>();
        }

        if (environment.IsDevelopment()
            && configuration.GetValue($"{LocalDevFixtureOptions.SectionName}:Enabled", false))
        {
            services.AddScoped<LocalDevCatalogFixtureSeeder>();
            services.AddScoped<LocalDevAccountFixtureSeeder>();
        }

        if (environment.IsDevelopment()
            && configuration.GetValue($"{DemoCatalogOptions.SectionName}:Enabled", false))
        {
            services.AddScoped<DemoCatalogSeeder>();
        }

        services.AddScoped<ICustomerIdentityLookup, PermixaCustomerIdentityLookup>();
        services.AddScoped<ICustomerIdentityCompensation, PermixaCustomerIdentityCompensation>();
        services.AddScoped<ICustomerEmailConfirmationGateway, PermixaCustomerEmailConfirmationGateway>();
        services.AddScoped<ICustomerPasswordResetGateway, PermixaCustomerPasswordResetGateway>();
        services.AddScoped<ICustomerEmailChangeGateway, PermixaCustomerEmailChangeGateway>();
        services.AddScoped<RegisterCustomerOrchestrator>();

        return services;
    }

    private static void RegisterEmailDelivery(
        IServiceCollection services,
        IHostEnvironment environment,
        bool requireConfirmedEmail,
        EmailDeliveryHostOptions email)
    {
        var frontend = (email.FrontendPublicUrl ?? string.Empty).Trim().TrimEnd('/');
        var hasFrontend = Uri.TryCreate(frontend, UriKind.Absolute, out _);
        var hasFrom = !string.IsNullOrWhiteSpace(email.FromEmail);
        var useCapturing = email.UseCapturingSender && !environment.IsProduction();
        var hasResendKey = !string.IsNullOrWhiteSpace(email.Resend.ApiKey);

        if (environment.IsProduction() && requireConfirmedEmail)
        {
            if (!hasFrontend || !hasFrom || (!hasResendKey && !useCapturing))
            {
                throw new InvalidOperationException(
                    "Production email verification requires Email:FrontendPublicUrl, Email:FromEmail, " +
                    "and Email:Resend:ApiKey (UseCapturingSender is not allowed in Production).");
            }
        }

        if (!hasFrontend || !hasFrom || (!useCapturing && !hasResendKey))
        {
            services.AddSingleton<Permixa.Application.Verification.Abstractions.IVerificationDispatcher,
                UnconfiguredVerificationDispatcher>();
            return;
        }

        services.AddPermixaEmailDelivery(o =>
        {
            o.FromEmail = email.FromEmail.Trim();
            o.FromName = string.IsNullOrWhiteSpace(email.FromName) ? email.FromEmail.Trim() : email.FromName.Trim();
            o.EmailConfirmationUrlTemplate =
                $"{frontend}/verify-email?challengeId={{challengeId}}&token={{token}}";
            // Required by Permixa email delivery validation; used by customer forgot/reset.
            o.PasswordResetUrlTemplate =
                $"{frontend}/reset-password?challengeId={{challengeId}}&token={{token}}";
            o.Branding.ApplicationName = string.IsNullOrWhiteSpace(email.Branding.ApplicationName)
                ? "ElectricalStore"
                : email.Branding.ApplicationName;
            o.Branding.CompanyName = email.Branding.CompanyName ?? string.Empty;
            o.Branding.LogoUrl = email.Branding.LogoUrl ?? string.Empty;
            o.Branding.SupportEmail = email.Branding.SupportEmail ?? string.Empty;
        });

        services.RemoveAll<IEmailTemplateRenderer>();
        services.AddSingleton<IEmailTemplateRenderer, ArabicEmailTemplateRenderer>();

        // Replace Permixa dispatcher so EmailChange uses host Arabic template + confirm URL.
        services.AddSingleton<EmailVerificationDispatcher>();
        services.RemoveAll<IVerificationDispatcher>();
        services.AddSingleton<IVerificationDispatcher, HostVerificationDispatcher>();

        if (useCapturing)
        {
            services.AddSingleton<CapturingEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<CapturingEmailSender>());
        }
        else
        {
            services.AddPermixaResendEmail(o => o.ApiKey = email.Resend.ApiKey);
        }
    }

    private static string RequireConfig(IConfiguration configuration, string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"Missing configuration '{key}'. Use User Secrets / environment / scripts/init-dev-secrets.");
}
