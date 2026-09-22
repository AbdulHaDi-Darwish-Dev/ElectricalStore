using ElectricalStore.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElectricalStore.Infrastructure.Persistence.Configurations;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("CustomerProfiles");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .ValueGeneratedNever();

        builder.Property(x => x.FullName)
            .HasMaxLength(CustomerProfile.FullNameMaxLength)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("IX_CustomerProfiles_UserId");
    }
}
