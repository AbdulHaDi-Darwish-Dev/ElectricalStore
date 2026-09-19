using ElectricalStore.Domain.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("DeliveryZones");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(DeliveryZone.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(DeliveryZone.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Fee)
            .HasPrecision(DeliveryZone.FeePrecision, DeliveryZone.FeeScale)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_DeliveryZones_NormalizedName");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_DeliveryZones_IsActive");
    }
}
