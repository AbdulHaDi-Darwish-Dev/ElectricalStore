namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Host email delivery / public-link configuration (secrets via env / user-secrets).
/// </summary>
public sealed class EmailDeliveryHostOptions
{
    public const string SectionName = "Email";

    /// <summary>Public storefront origin used in verification links (no trailing slash required).</summary>
    public string FrontendPublicUrl { get; set; } = "http://localhost:3100";

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public EmailBrandingHostOptions Branding { get; set; } = new();

    public ResendHostOptions Resend { get; set; } = new();

    /// <summary>
    /// Development/test only: capture outbound email in-memory instead of calling Resend.
    /// Never enable in Production.
    /// </summary>
    public bool UseCapturingSender { get; set; }

    /// <summary>Optional override; defaults to Permixa UrlTokenLifetime (1 hour).</summary>
    public int? UrlTokenLifetimeMinutes { get; set; }

    /// <summary>Optional override; defaults to Permixa ResendCooldown (1 minute).</summary>
    public int? ResendCooldownSeconds { get; set; }
}

public sealed class EmailBrandingHostOptions
{
    public string ApplicationName { get; set; } = "ElectricalStore";

    public string CompanyName { get; set; } = string.Empty;

    public string LogoUrl { get; set; } = string.Empty;

    public string SupportEmail { get; set; } = string.Empty;
}

public sealed class ResendHostOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
