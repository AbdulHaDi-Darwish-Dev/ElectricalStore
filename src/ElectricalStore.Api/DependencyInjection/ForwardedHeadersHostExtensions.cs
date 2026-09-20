using System.Net;
using ElectricalStore.Api.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Api.DependencyInjection;

/// <summary>
/// Narrow host wiring for Microsoft ForwardedHeadersMiddleware.
/// Trusts only explicitly configured KnownProxies / KnownNetworks.
/// </summary>
public static class ForwardedHeadersHostExtensions
{
    public static IServiceCollection AddElectricalStoreForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersHostOptions>(
            configuration.GetSection(ForwardedHeadersHostOptions.SectionName));

        // No-op unless Testing:AllowConnectingIpOverride=true (IntegrationTests).
        services.AddSingleton<IStartupFilter, TestConnectingIpStartupFilter>();

        return services;
    }

    /// <summary>
    /// Registers ForwardedHeadersMiddleware when Enabled and at least one trust entry exists.
    /// Must run before request logging, authentication, rate limiting, and authorization.
    /// </summary>
    public static IApplicationBuilder UseElectricalStoreForwardedHeaders(
        this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ElectricalStore.ForwardedHeaders");
        var hostOptions = configuration
            .GetSection(ForwardedHeadersHostOptions.SectionName)
            .Get<ForwardedHeadersHostOptions>()
            ?? new ForwardedHeadersHostOptions();

        if (!hostOptions.Enabled)
        {
            logger.LogInformation(
                "ForwardedHeaders disabled (ForwardedHeaders:Enabled=false). " +
                "Connection.RemoteIpAddress remains the direct TCP peer.");
            return app;
        }

        var proxies = ParseProxies(hostOptions.KnownProxies, logger);
        var networks = ParseNetworks(hostOptions.KnownNetworks, logger);
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (proxies.Count == 0 && networks.Count == 0)
        {
            // Production fail-closed: Enabled without an explicit trust list must not boot.
            // Non-Production: refuse to register middleware (do not trust arbitrary XFF).
            if (env.IsProduction())
            {
                throw new InvalidOperationException(
                    "ForwardedHeaders:Enabled=true in Production but KnownProxies/KnownNetworks are empty. " +
                    "Set explicit KnownProxies for nginx and Next.js (see docker-compose.prod.yml).");
            }

            logger.LogWarning(
                "ForwardedHeaders:Enabled=true but KnownProxies/KnownNetworks are empty. " +
                "Middleware NOT registered — refusing to trust forwarded headers.");
            return app;
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Never inherit framework loopback defaults when operators supply an explicit list.
            ForwardLimit = 1
        };
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();

        foreach (var proxy in proxies)
            options.KnownProxies.Add(proxy);

        foreach (var network in networks)
            options.KnownNetworks.Add(network);

        logger.LogInformation(
            "ForwardedHeaders enabled ({Environment}). Trusted proxies={ProxyCount}, networks={NetworkCount}. Headers=X-Forwarded-For,X-Forwarded-Proto. ForwardLimit=1.",
            env.EnvironmentName,
            proxies.Count,
            networks.Count);

        return app.UseForwardedHeaders(options);
    }

    private static List<IPAddress> ParseProxies(string[] values, ILogger logger)
    {
        var list = new List<IPAddress>();
        foreach (var raw in values ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (IPAddress.TryParse(raw.Trim(), out var ip))
                list.Add(ip);
            else
                logger.LogWarning("Ignoring invalid ForwardedHeaders:KnownProxies entry '{Value}'.", raw);
        }

        return list;
    }

    private static List<Microsoft.AspNetCore.HttpOverrides.IPNetwork> ParseNetworks(
        string[] values,
        ILogger logger)
    {
        var list = new List<Microsoft.AspNetCore.HttpOverrides.IPNetwork>();
        foreach (var raw in values ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            var slash = trimmed.IndexOf('/');
            if (slash <= 0
                || slash == trimmed.Length - 1
                || !IPAddress.TryParse(trimmed[..slash], out var prefix)
                || !int.TryParse(trimmed[(slash + 1)..], out var prefixLength)
                || prefixLength < 0)
            {
                logger.LogWarning("Ignoring invalid ForwardedHeaders:KnownNetworks entry '{Value}'.", raw);
                continue;
            }

            try
            {
                list.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ignoring invalid ForwardedHeaders:KnownNetworks entry '{Value}'.", raw);
            }
        }

        return list;
    }
}
