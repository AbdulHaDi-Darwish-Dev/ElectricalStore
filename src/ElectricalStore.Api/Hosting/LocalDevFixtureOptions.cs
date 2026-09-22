namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Development-only catalog and account fixture settings (E2E / local Playwright).
/// Passwords belong in user-secrets, not committed configuration.
/// </summary>
public sealed class LocalDevFixtureOptions
{
    public const string SectionName = "LocalDevFixtures";

    public bool Enabled { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string CustomerUserName { get; set; } = string.Empty;

    public string CustomerPassword { get; set; } = string.Empty;

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminUserName { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public string LimitedEmail { get; set; } = string.Empty;

    public string LimitedUserName { get; set; } = string.Empty;

    public string LimitedPassword { get; set; } = string.Empty;
}
