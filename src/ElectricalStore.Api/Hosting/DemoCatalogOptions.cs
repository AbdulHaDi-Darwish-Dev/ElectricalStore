namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Development-only rich Arabic demo catalog (human storefront review).
/// Separate from <see cref="LocalDevFixtureOptions"/> (deterministic E2E fixtures).
/// </summary>
public sealed class DemoCatalogOptions
{
    public const string SectionName = "DemoCatalog";

    /// <summary>Must stay false unless Development intentionally enables it.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Absolute public base for static demo media URLs, e.g. http://localhost:5180.
    /// Images are served from /demo-catalog/... when files exist under wwwroot/demo-catalog.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5180";
}
