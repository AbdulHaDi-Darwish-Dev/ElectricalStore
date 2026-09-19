using ElectricalStore.Domain.Shipping;
using Xunit;

namespace ElectricalStore.Domain.Tests.Shipping;

public sealed class DeliveryZoneTests
{
    [Fact]
    public void Create_Valid_TrimsName_AndSetsFee()
    {
        var zone = DeliveryZone.Create("  Azizieh  ", 1500.5m, true);

        Assert.Equal("Azizieh", zone.Name);
        Assert.Equal("AZIZIEH", zone.NormalizedName);
        Assert.Equal(1500.5m, zone.Fee);
        Assert.True(zone.IsActive);
    }

    [Fact]
    public void Create_ZeroFee_Allowed()
    {
        var zone = DeliveryZone.Create("Free Zone", 0m, true);
        Assert.Equal(0m, zone.Fee);
    }

    [Fact]
    public void Create_NegativeFee_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeliveryZone.Create("Bad", -1m, true));
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => DeliveryZone.Create("  ", 10m, true));
    }

    [Fact]
    public void Update_ChangesNameAndFee()
    {
        var zone = DeliveryZone.Create("Old", 100m, true);
        zone.Update("New", 200m);

        Assert.Equal("New", zone.Name);
        Assert.Equal("NEW", zone.NormalizedName);
        Assert.Equal(200m, zone.Fee);
    }

    [Fact]
    public void Activate_Deactivate_Toggle()
    {
        var zone = DeliveryZone.Create("Z", 1m, false);
        zone.Activate();
        Assert.True(zone.IsActive);
        zone.Deactivate();
        Assert.False(zone.IsActive);
    }
}
