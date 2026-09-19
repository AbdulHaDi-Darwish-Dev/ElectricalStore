using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(Product.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Product.DescriptionMaxLength);

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Variants)
            .WithOne()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Variants)
            .HasField("_variants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.CategoryId)
            .HasDatabaseName("IX_Products_CategoryId");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Products_IsActive");
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    /// <summary>Monetary precision for SYP MVP (and safe general money handling). Scale 2.</summary>
    public const int PricePrecision = 18;
    public const int PriceScale = 2;

    /// <summary>Quantity increment precision (supports 0.5 m and finer store increments).</summary>
    public const int QuantityIncrementPrecision = 18;
    public const int QuantityIncrementScale = 3;

    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(ProductVariant.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(ProductVariant.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasMaxLength(ProductVariant.SkuMaxLength)
            .IsRequired();

        builder.Property(x => x.NormalizedSku)
            .HasMaxLength(ProductVariant.SkuMaxLength)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(PricePrecision, PriceScale)
            .IsRequired();

        builder.Property(x => x.SellingUnit)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.QuantityIncrement)
            .HasPrecision(QuantityIncrementPrecision, QuantityIncrementScale)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.NormalizedSku)
            .IsUnique()
            .HasDatabaseName("IX_ProductVariants_NormalizedSku");

        builder.HasIndex(x => new { x.ProductId, x.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_ProductVariants_ProductId_NormalizedName");

        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_ProductVariants_ProductId");
    }
}

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasMaxLength(ProductImage.StorageKeyMaxLength)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasMaxLength(ProductImage.UrlMaxLength)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        builder.HasIndex(x => new { x.ProductId, x.SortOrder })
            .HasDatabaseName("IX_ProductImages_ProductId_SortOrder");
    }
}
