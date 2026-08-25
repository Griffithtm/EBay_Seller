using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequest");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.OrderId).HasColumnName("orderId");
        builder.Property(r => r.UserId).HasColumnName("userId");
        builder.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.PreferredResolution).HasColumnName("preferredResolution").HasMaxLength(50);
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(r => r.ReturnCarrier).HasColumnName("returnCarrier").HasMaxLength(100);
        builder.Property(r => r.TrackingNumber).HasColumnName("trackingNumber").HasMaxLength(100);
        builder.Property(r => r.BuyerShippedAt).HasColumnName("buyerShippedAt");
        builder.Property(r => r.DeliveredAt).HasColumnName("deliveredAt");
        builder.Property(r => r.RestockingFee).HasColumnName("restockingFee").HasColumnType("decimal(18,2)");
        builder.Property(r => r.RefundAmount).HasColumnName("refundAmount").HasColumnType("decimal(18,2)");
        builder.Property(r => r.SellerNote).HasColumnName("sellerNote").HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).HasColumnName("createdAt");
        builder.Property(r => r.UpdatedAt).HasColumnName("updatedAt");

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany(u => u.ReturnRequests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.Status);
    }
}
