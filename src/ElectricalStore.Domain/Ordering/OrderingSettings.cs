namespace ElectricalStore.Domain.Ordering;

/// <summary>
/// Singleton business settings for Ordering (MVP). Not a generic settings platform.
/// </summary>
public sealed class OrderingSettings
{
    /// <summary>Well-known singleton row id.</summary>
    public static readonly Guid SingletonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001");

    public Guid Id { get; private set; }

    /// <summary>Minimum merchandise subtotal (excludes shipping). SYP decimal(18,2).</summary>
    public decimal MinimumMerchandiseSubtotal { get; private set; }

    private OrderingSettings()
    {
    }

    public static OrderingSettings CreateDefault(decimal minimumMerchandiseSubtotal = 0m)
    {
        var settings = new OrderingSettings
        {
            Id = SingletonId,
            MinimumMerchandiseSubtotal = 0m
        };
        settings.SetMinimumMerchandiseSubtotal(minimumMerchandiseSubtotal);
        return settings;
    }

    public void SetMinimumMerchandiseSubtotal(decimal amount)
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Minimum order amount cannot be negative.");

        MinimumMerchandiseSubtotal = Money.Round(amount);
    }
}
