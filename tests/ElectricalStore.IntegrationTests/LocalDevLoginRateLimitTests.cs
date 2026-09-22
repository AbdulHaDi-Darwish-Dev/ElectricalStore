using ElectricalStore.Api.Hosting;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class LocalDevLoginRateLimitTests
{
    [Theory]
    [InlineData(false, false, LocalDevLoginRateLimits.DefaultPermitLimit)]
    [InlineData(false, true, LocalDevLoginRateLimits.DefaultPermitLimit)]
    [InlineData(true, false, LocalDevLoginRateLimits.DefaultPermitLimit)]
    [InlineData(true, true, LocalDevLoginRateLimits.LocalDevFixturesPermitLimit)]
    public void ResolvePermitLimit_RequiresDevelopmentAndFixtures(
        bool isDevelopment,
        bool fixturesEnabled,
        int expected)
    {
        Assert.Equal(
            expected,
            LocalDevLoginRateLimits.ResolvePermitLimit(isDevelopment, fixturesEnabled));
    }

    [Fact]
    public void Production_CannotReceiveFixtureWidening()
    {
        // Production is never Development — even if someone mis-sets LocalDevFixtures:Enabled.
        Assert.Equal(
            LocalDevLoginRateLimits.DefaultPermitLimit,
            LocalDevLoginRateLimits.ResolvePermitLimit(
                isDevelopment: false,
                localDevFixturesEnabled: true));
    }
}
