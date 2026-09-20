using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectricalStore.Api.DependencyInjection;

/// <summary>
/// Host CORS policy for the approved Next.js frontend origin(s).
/// Origins are configuration-driven; empty/missing config denies all origins (no AllowAnyOrigin).
/// </summary>
public static class FrontendCorsServiceCollectionExtensions
{
    public const string FrontendPolicyName = "Frontend";

    public const string SectionName = "Cors";

    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection($"{SectionName}:AllowedOrigins").Get<string[]>()
                      ?? Array.Empty<string>();

        var allowed = origins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendPolicyName, policy =>
            {
                // Explicit deny when unconfigured — never AllowAnyOrigin.
                if (allowed.Length == 0)
                    policy.SetIsOriginAllowed(_ => false);
                else
                    policy.WithOrigins(allowed);

                policy
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                    .WithHeaders(
                        "Authorization",
                        "Content-Type",
                        "Idempotency-Key",
                        "X-Order-Token")
                    .WithExposedHeaders("Retry-After");
                // AllowCredentials intentionally omitted: API writes no auth cookies;
                // refresh cookie will live on the Next.js origin; browser calls use Bearer.
            });
        });

        return services;
    }
}
