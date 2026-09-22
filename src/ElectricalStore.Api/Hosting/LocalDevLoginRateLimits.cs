namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Login sliding-window permit limits. The widened E2E budget is Development + LocalDevFixtures only.
/// </summary>
public static class LocalDevLoginRateLimits
{
    public const int DefaultPermitLimit = 20;
    public const int LocalDevFixturesPermitLimit = 200;

    public static int ResolvePermitLimit(bool isDevelopment, bool localDevFixturesEnabled) =>
        isDevelopment && localDevFixturesEnabled
            ? LocalDevFixturesPermitLimit
            : DefaultPermitLimit;
}
