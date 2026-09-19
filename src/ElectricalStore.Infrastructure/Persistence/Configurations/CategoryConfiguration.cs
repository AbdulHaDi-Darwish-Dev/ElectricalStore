using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ElectricalStore.Domain.Catalog.Categories;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(Category.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(Category.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Category.DescriptionMaxLength);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ImageStorageKey)
            .HasMaxLength(Category.ImageStorageKeyMaxLength);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(Category.ImageUrlMaxLength);

        // Guarantees case-insensitive uniqueness ("Cables" / "cables" / "CABLES").
        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Categories_NormalizedName");
    }
}
