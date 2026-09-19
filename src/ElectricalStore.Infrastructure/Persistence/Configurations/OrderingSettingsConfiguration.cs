using ElectricalStore.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class OrderingSettingsConfiguration : IEntityTypeConfiguration<OrderingSettings>
{
    public void Configure(EntityTypeBuilder<OrderingSettings> builder)
    {
        builder.ToTable("OrderingSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.MinimumMerchandiseSubtotal)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();
    }
}

public sealed class OrderPlacementIdempotencyConfiguration : IEntityTypeConfiguration<OrderPlacementIdempotency>
{
    public void Configure(EntityTypeBuilder<OrderPlacementIdempotency> builder)
    {
        builder.ToTable("OrderPlacementIdempotencies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.KeyHash)
            .HasMaxLength(OrderPlacementIdempotency.KeyHashMaxLength)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasMaxLength(OrderPlacementIdempotency.ScopeMaxLength)
            .IsRequired();

        builder.Property(x => x.ProtectedGuestAccessToken)
            .HasMaxLength(OrderPlacementIdempotency.ProtectedGuestTokenMaxLength);

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.Scope, x.KeyHash })
            .IsUnique()
            .HasDatabaseName("IX_OrderPlacementIdempotencies_Scope_KeyHash");

        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_OrderPlacementIdempotencies_OrderId");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_OrderPlacementIdempotencies_ExpiresAtUtc");
    }
}
