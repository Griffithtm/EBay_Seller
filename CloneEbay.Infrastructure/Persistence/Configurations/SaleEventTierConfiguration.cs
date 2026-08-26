using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class SaleEventTierConfiguration : IEntityTypeConfiguration<SaleEventTier>
{
    public void Configure(EntityTypeBuilder<SaleEventTier> builder)
    {
        builder.ToTable("SaleEventTier");
        builder.HasKey(tier => tier.Id);
        builder.Property(tier => tier.Id).HasColumnName("id");
        builder.Property(tier => tier.SaleEventId).HasColumnName("saleEventId");
        builder.Property(tier => tier.DiscountType).HasColumnName("discountType").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(tier => tier.DiscountValue).HasColumnName("discountValue").HasPrecision(10, 2);
        builder.Property(tier => tier.Priority).HasColumnName("priority");
        builder.Property(tier => tier.Label).HasColumnName("label").HasMaxLength(100);

        builder.HasIndex(tier => tier.SaleEventId);
        builder.HasOne(tier => tier.SaleEvent)
            .WithMany(saleEvent => saleEvent.Tiers)
            .HasForeignKey(tier => tier.SaleEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
