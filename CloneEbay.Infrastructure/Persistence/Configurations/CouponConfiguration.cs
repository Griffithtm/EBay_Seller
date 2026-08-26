using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        // Table name must be set explicitly: the DbSet property is "Coupons" (plural), but the
        // physical table (from the original raw-SQL schema) is "Coupon" (singular) — without this,
        // EF's default convention derives the table name from the DbSet property and queries a
        // table that doesn't exist.
        builder.ToTable("Coupon");
        // Legacy check constraints (CK_Coupon_DiscountPercent/_DateRange/_MaxUsage/_UsageCount)
        // stay in place at the DB level — they only fire when the guarded column is non-null,
        // and every new-model coupon still satisfies them (date range, non-negative usage).
        builder.HasKey(coupon => coupon.Id);
        builder.Property(coupon => coupon.Id).HasColumnName("id");
        builder.Property(coupon => coupon.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(coupon => coupon.DiscountPercent).HasColumnName("discountPercent").HasPrecision(5, 2);
        builder.Property(coupon => coupon.ProductId).HasColumnName("productId");
        builder.Property(coupon => coupon.StartDate).HasColumnName("startDate");
        builder.Property(coupon => coupon.EndDate).HasColumnName("endDate");
        builder.Property(coupon => coupon.UsageLimit).HasColumnName("maxUsage");
        builder.Property(coupon => coupon.UsedCount).HasColumnName("usageCount");
        builder.Property(coupon => coupon.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(coupon => coupon.UpdatedAtUtc).HasColumnName("updatedAtUtc");

        builder.Property(coupon => coupon.CouponTypeId).HasColumnName("couponTypeId");
        builder.Property(coupon => coupon.CategoryId).HasColumnName("categoryId");
        builder.Property(coupon => coupon.SellerId).HasColumnName("sellerId");
        builder.Property(coupon => coupon.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(coupon => coupon.DiscountValue).HasColumnName("discountValue").HasPrecision(10, 2);
        builder.Property(coupon => coupon.DiscountUnit).HasColumnName("discountUnit").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(coupon => coupon.MaxDiscount).HasColumnName("maxDiscount").HasPrecision(10, 2);
        builder.Property(coupon => coupon.UsagePerUser).HasColumnName("usagePerUser");
        builder.Property(coupon => coupon.MinimumOrderValue).HasColumnName("minimumOrderValue").HasPrecision(10, 2);
        builder.Property(coupon => coupon.ApplicablePriceMin).HasColumnName("applicablePriceMin").HasPrecision(10, 2);
        builder.Property(coupon => coupon.ApplicablePriceMax).HasColumnName("applicablePriceMax").HasPrecision(10, 2);
        builder.Property(coupon => coupon.IsActive).HasColumnName("isActive");

        builder.HasIndex(coupon => coupon.SellerId);
        builder.HasOne(coupon => coupon.CouponType)
            .WithMany(couponType => couponType.Coupons)
            .HasForeignKey(coupon => coupon.CouponTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(coupon => coupon.Seller)
            .WithMany()
            .HasForeignKey(coupon => coupon.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(coupon => coupon.Category)
            .WithMany()
            .HasForeignKey(coupon => coupon.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(coupon => coupon.Product)
            .WithMany(product => product.Coupons)
            .HasForeignKey(coupon => coupon.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
