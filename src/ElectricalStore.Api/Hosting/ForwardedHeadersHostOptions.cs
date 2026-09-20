namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Host-only ForwardedHeaders trust boundary. Disabled unless explicitly enabled with
/// KnownProxies and/or KnownNetworks — never trust arbitrary X-Forwarded-For.
/// </summary>
public sealed class ForwardedHeadersHostOptions
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// When false (default for Production baseline), ForwardedHeadersMiddleware is not registered.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Immediate trusted hop IP addresses (e.g. Next.js / reverse-proxy container IP).
    /// </summary>
    public string[] KnownProxies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Immediate trusted hop CIDR networks (e.g. Docker bridge). Format: prefix/prefixLength
    /// such as <c>10.0.0.0/8</c>.
    /// </summary>
    public string[] KnownNetworks { get; set; } = Array.Empty<string>();
}
