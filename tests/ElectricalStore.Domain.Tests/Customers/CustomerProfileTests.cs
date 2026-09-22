using ElectricalStore.Domain.Customers;
using Xunit;

namespace ElectricalStore.Domain.Tests.Customers;

public sealed class CustomerProfileTests
{
    [Fact]
    public void Create_AcceptsArabicFullNameWithSpaces()
    {
        var now = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
        var profile = CustomerProfile.Create(
            Guid.NewGuid(),
            "  عبدالهادي درويش  ",
            now);

        Assert.Equal("عبدالهادي درويش", profile.FullName);
        Assert.Equal(now, profile.CreatedAtUtc);
        Assert.Equal(now, profile.UpdatedAtUtc);
    }

    [Fact]
    public void Create_RejectsWhitespaceOnly()
    {
        Assert.Throws<ArgumentException>(() =>
            CustomerProfile.Create(Guid.NewGuid(), "   ", DateTime.UtcNow));
    }

    [Fact]
    public void UpdateFullName_UpdatesTimestamp()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updated = created.AddDays(1);
        var profile = CustomerProfile.Create(Guid.NewGuid(), "Alice", created);
        profile.UpdateFullName("Alice Smith", updated);
        Assert.Equal("Alice Smith", profile.FullName);
        Assert.Equal(updated, profile.UpdatedAtUtc);
    }
}
