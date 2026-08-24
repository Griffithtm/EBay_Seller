using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Address");
        builder.HasKey(address => address.Id);
        builder.Property(address => address.Id).HasColumnName("id");
        builder.Property(address => address.UserId).HasColumnName("userId");
        builder.Property(address => address.FullName).HasColumnName("fullName").HasMaxLength(100);
        builder.Property(address => address.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(address => address.Street).HasColumnName("street").HasMaxLength(100);
        builder.Property(address => address.City).HasColumnName("city").HasMaxLength(50);
        builder.Property(address => address.State).HasColumnName("state").HasMaxLength(50);
        builder.Property(address => address.Country).HasColumnName("country").HasMaxLength(50);
        builder.Property(address => address.IsDefault).HasColumnName("isDefault");

        builder.HasOne(address => address.User)
            .WithMany()
            .HasForeignKey(address => address.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
