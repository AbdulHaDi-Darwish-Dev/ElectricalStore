using ElectricalStore.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .HasMaxLength(Order.OrderNumberMaxLength)
            .IsRequired();

        builder.Property(x => x.GuestAccessTokenHash)
            .HasMaxLength(Order.GuestTokenHashMaxLength);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CustomerName)
            .HasMaxLength(Order.CustomerNameMaxLength)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(Order.PhoneMaxLength)
            .IsRequired();

        builder.Property(x => x.AddressText)
            .HasMaxLength(Order.AddressMaxLength)
            .IsRequired();

        builder.Property(x => x.CustomerNote)
            .HasMaxLength(Order.NoteMaxLength);

        builder.Property(x => x.DeliveryZoneName)
            .HasMaxLength(Order.ZoneNameMaxLength)
            .IsRequired();

        builder.Property(x => x.ShippingFee)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        builder.Property(x => x.MerchandiseSubtotal)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        builder.Property(x => x.Total)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        builder.Property(x => x.AppliedMinimumOrderAmount)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(Order.CancellationReasonMaxLength);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.ModificationAudits)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.ModificationAudits)
            .HasField("_modificationAudits")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_OrderNumber");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Orders_UserId");

        builder.HasIndex(x => x.GuestAccessTokenHash)
            .IsUnique()
            .HasFilter("[GuestAccessTokenHash] IS NOT NULL")
            .HasDatabaseName("IX_Orders_GuestAccessTokenHash");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Orders_Status");

        builder.HasIndex(x => x.PaymentStatus)
            .HasDatabaseName("IX_Orders_PaymentStatus");

        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("IX_Orders_CreatedAtUtc");

        builder.HasIndex(x => x.Phone)
            .HasDatabaseName("IX_Orders_Phone");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Orders_OneIdentityMode",
            "([UserId] IS NOT NULL AND [GuestAccessTokenHash] IS NULL) OR ([UserId] IS NULL AND [GuestAccessTokenHash] IS NOT NULL)"));
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductName)
            .HasMaxLength(OrderItem.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.VariantName)
            .HasMaxLength(OrderItem.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasMaxLength(OrderItem.SkuMaxLength)
            .IsRequired();

        builder.Property(x => x.SellingUnit)
            .HasMaxLength(OrderItem.SellingUnitMaxLength)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(QuantityRules.QuantityPrecision, QuantityRules.QuantityScale)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        builder.Property(x => x.LineTotal)
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");

        builder.HasIndex(x => x.ProductVariantId)
            .HasDatabaseName("IX_OrderItems_ProductVariantId");
    }
}

public sealed class OrderModificationAuditConfiguration : IEntityTypeConfiguration<OrderModificationAudit>
{
    public void Configure(EntityTypeBuilder<OrderModificationAudit> builder)
    {
        builder.ToTable("OrderModificationAudits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Reason)
            .HasMaxLength(OrderModificationAudit.ReasonMaxLength)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(OrderModificationAudit.SummaryMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_OrderModificationAudits_OrderId");
    }
}
