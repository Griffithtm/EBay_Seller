using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class CouponConditionConfiguration : IEntityTypeConfiguration<CouponCondition>
{
    public void Configure(EntityTypeBuilder<CouponCondition> builder)
    {
        builder.ToTable("CouponCondition");
        builder.HasKey(condition => condition.Id);
        builder.Property(condition => condition.Id).HasColumnName("id");
        builder.Property(condition => condition.CouponId).HasColumnName("couponId");
        builder.Property(condition => condition.BuyQuantity).HasColumnName("buyQuantity");
        builder.Property(condition => condition.GetQuantity).HasColumnName("getQuantity");
        builder.Property(condition => condition.GetDiscountPercent).HasColumnName("getDiscountPercent").HasPrecision(5, 2);
        builder.Property(condition => condition.SaveEveryAmount).HasColumnName("saveEveryAmount").HasPrecision(10, 2);
        builder.Property(condition => condition.SaveEveryItems).HasColumnName("saveEveryItems");
        builder.Property(condition => condition.ConditionDescription).HasColumnName("conditionDescription").HasMaxLength(255);

        builder.HasIndex(condition => condition.CouponId);
        builder.HasOne(condition => condition.Coupon)
            .WithMany(coupon => coupon.Conditions)
            .HasForeignKey(condition => condition.CouponId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
