using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class SaleEventProductConfiguration : IEntityTypeConfiguration<SaleEventProduct>
{
    public void Configure(EntityTypeBuilder<SaleEventProduct> builder)
    {
        builder.ToTable("SaleEventProduct");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.SaleEventId).HasColumnName("saleEventId");
        builder.Property(item => item.TierId).HasColumnName("tierId");
        builder.Property(item => item.ProductId).HasColumnName("productId");

        builder.HasIndex(item => new { item.SaleEventId, item.ProductId }).IsUnique();
        builder.HasIndex(item => item.TierId);
        builder.HasOne(item => item.SaleEvent)
            .WithMany(saleEvent => saleEvent.SaleEventProducts)
            .HasForeignKey(item => item.SaleEventId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Tier)
            .WithMany(tier => tier.SaleEventProducts)
            .HasForeignKey(item => item.TierId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
