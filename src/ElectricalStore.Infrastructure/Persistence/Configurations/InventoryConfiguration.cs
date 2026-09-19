using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(x => x.ProductVariantId);

        builder.Property(x => x.ProductVariantId)
            .ValueGeneratedNever();

        builder.Property(x => x.OnHand)
            .HasPrecision(InventoryItem.QuantityPrecision, InventoryItem.QuantityScale)
            .IsRequired();

        builder.Property(x => x.Reserved)
            .HasPrecision(InventoryItem.QuantityPrecision, InventoryItem.QuantityScale)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_InventoryItems_OnHand_NonNegative",
                "[OnHand] >= 0");
            t.HasCheckConstraint(
                "CK_InventoryItems_Reserved_NonNegative",
                "[Reserved] >= 0");
            t.HasCheckConstraint(
                "CK_InventoryItems_Reserved_Le_OnHand",
                "[Reserved] <= [OnHand]");
        });
    }
}

public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductVariantId)
            .IsRequired();

        builder.Property(x => x.QuantityDelta)
            .HasPrecision(InventoryItem.QuantityPrecision, InventoryItem.QuantityScale)
            .IsRequired();

        builder.Property(x => x.OnHandBefore)
            .HasPrecision(InventoryItem.QuantityPrecision, InventoryItem.QuantityScale)
            .IsRequired();

        builder.Property(x => x.OnHandAfter)
            .HasPrecision(InventoryItem.QuantityPrecision, InventoryItem.QuantityScale)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(InventoryAdjustment.ReasonMaxLength)
            .IsRequired();

        builder.Property(x => x.PerformedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProductVariantId)
            .HasDatabaseName("IX_InventoryAdjustments_ProductVariantId");

        builder.HasIndex(x => new { x.ProductVariantId, x.CreatedAtUtc })
            .HasDatabaseName("IX_InventoryAdjustments_ProductVariantId_CreatedAtUtc");
    }
}
