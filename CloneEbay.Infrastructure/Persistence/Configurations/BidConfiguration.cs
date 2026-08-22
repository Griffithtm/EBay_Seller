using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bid", table => table.HasCheckConstraint("CK_Bid_Amount_Positive", "[amount] > 0"));
        builder.HasKey(bid => bid.Id);
        builder.Property(bid => bid.Id).HasColumnName("id");
        builder.Property(bid => bid.ProductId).HasColumnName("productId");
        builder.Property(bid => bid.BidderId).HasColumnName("bidderId");
        builder.Property(bid => bid.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(bid => bid.BidTimeUtc).HasColumnName("bidTime");
        builder.HasIndex(bid => new { bid.ProductId, bid.Amount });
        builder.HasOne(bid => bid.Product)
            .WithMany(product => product.Bids)
            .HasForeignKey(bid => bid.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(bid => bid.Bidder)
            .WithMany(user => user.Bids)
            .HasForeignKey(bid => bid.BidderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
