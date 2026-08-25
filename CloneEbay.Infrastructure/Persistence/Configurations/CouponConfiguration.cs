using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupon", table =>
        {
            table.HasCheckConstraint("CK_Coupon_DiscountPercent", "[discountPercent] > 0 AND [discountPercent] <= 100");
            table.HasCheckConstraint("CK_Coupon_DateRange", "[endDate] > [startDate]");
            table.HasCheckConstraint("CK_Coupon_MaxUsage", "[maxUsage] >= 1");
            table.HasCheckConstraint("CK_Coupon_UsageCount", "[usageCount] >= 0");
        });
        builder.HasKey(coupon => coupon.Id);
        builder.Property(coupon => coupon.Id).HasColumnName("id");
        builder.Property(coupon => coupon.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(coupon => coupon.DiscountPercent).HasColumnName("discountPercent").HasPrecision(5, 2);
        builder.Property(coupon => coupon.StartDateUtc).HasColumnName("startDate");
        builder.Property(coupon => coupon.EndDateUtc).HasColumnName("endDate");
        builder.Property(coupon => coupon.MaxUsage).HasColumnName("maxUsage");
        builder.Property(coupon => coupon.UsageCount).HasColumnName("usageCount");
        builder.Property(coupon => coupon.ProductId).HasColumnName("productId");
        builder.Property(coupon => coupon.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(coupon => coupon.UpdatedAtUtc).HasColumnName("updatedAtUtc");

        builder.HasIndex(coupon => new { coupon.ProductId, coupon.Code }).IsUnique();
        builder.HasOne(coupon => coupon.Product)
            .WithMany(product => product.Coupons)
            .HasForeignKey(coupon => coupon.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
