using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ShippingInfoConfiguration : IEntityTypeConfiguration<ShippingInfo>
{
    public void Configure(EntityTypeBuilder<ShippingInfo> builder)
    {
        builder.ToTable("ShippingInfo");
        builder.HasKey(info => info.Id);
        builder.Property(info => info.Id).HasColumnName("id");
        builder.Property(info => info.OrderId).HasColumnName("orderId");
        builder.Property(info => info.Carrier).HasColumnName("carrier").HasMaxLength(100);
        builder.Property(info => info.TrackingNumber).HasColumnName("trackingNumber").HasMaxLength(100);
        builder.Property(info => info.Status).HasColumnName("status").HasMaxLength(50);
        builder.Property(info => info.EstimatedArrivalUtc).HasColumnName("estimatedArrival");

        builder.HasOne(info => info.Order)
            .WithOne(order => order.ShippingInfo)
            .HasForeignKey<ShippingInfo>(info => info.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
