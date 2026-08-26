using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("OrderTable");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).HasColumnName("id");
        builder.Property(order => order.BuyerId).HasColumnName("buyerId");
        builder.Property(order => order.AddressId).HasColumnName("addressId");
        builder.Property(order => order.OrderDateUtc).HasColumnName("orderDate");
        builder.Property(order => order.TotalPrice).HasColumnName("totalPrice").HasPrecision(18, 2);
        builder.Property(order => order.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(order => order.AuctionProductId).HasColumnName("auctionProductId");
        builder.Property(order => order.FinalValueFee).HasColumnName("finalValueFee").HasPrecision(18, 2);
        builder.Property(order => order.PayoutStatus).HasColumnName("payoutStatus").HasMaxLength(20).HasDefaultValue("Pending");
        builder.HasIndex(order => order.AuctionProductId)
            .IsUnique()
            .HasFilter("[auctionProductId] IS NOT NULL");
        builder.HasOne(order => order.Buyer)
            .WithMany(user => user.Orders)
            .HasForeignKey(order => order.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.AuctionProduct)
            .WithOne(product => product.AuctionOrder)
            .HasForeignKey<Order>(order => order.AuctionProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.Address)
            .WithMany()
            .HasForeignKey(order => order.AddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
