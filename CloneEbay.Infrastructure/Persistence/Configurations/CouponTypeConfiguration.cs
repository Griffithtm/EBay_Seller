using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class CouponTypeConfiguration : IEntityTypeConfiguration<CouponType>
{
    public void Configure(EntityTypeBuilder<CouponType> builder)
    {
        builder.ToTable("CouponType");
        builder.HasKey(couponType => couponType.Id);
        builder.Property(couponType => couponType.Id).HasColumnName("id");
        builder.Property(couponType => couponType.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(couponType => couponType.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(couponType => couponType.IsActive).HasColumnName("isActive");
    }
}
