using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product", table =>
        {
            table.HasCheckConstraint("CK_Product_Price_Positive", "[price] > 0");
            table.HasCheckConstraint("CK_Product_AuctionEnd", "[isAuction] = 0 OR [auctionEndTime] IS NOT NULL");
        });
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).HasColumnName("id");
        builder.Property(product => product.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(product => product.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
        builder.Property(product => product.Price).HasColumnName("price").HasPrecision(18, 2);
        builder.Property(product => product.ImagesJson).HasColumnName("images").IsRequired();
        builder.Property(product => product.CategoryId).HasColumnName("categoryId");
        builder.Property(product => product.SellerId).HasColumnName("sellerId");
        builder.Property(product => product.IsAuction).HasColumnName("isAuction");
        builder.Property(product => product.AuctionEndTimeUtc).HasColumnName("auctionEndTime");
        builder.Property(product => product.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(product => product.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(product => product.UpdatedAtUtc).HasColumnName("updatedAtUtc");
        builder.Property(product => product.AuctionClosedAtUtc).HasColumnName("auctionClosedAtUtc");
        builder.Property(product => product.AuctionWinnerId).HasColumnName("auctionWinnerId");

        builder.HasIndex(product => new { product.SellerId, product.Status });
        builder.HasIndex(product => new { product.CategoryId, product.Status });
        builder.HasIndex(product => product.AuctionEndTimeUtc);
        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(product => product.Seller)
            .WithMany(user => user.Products)
            .HasForeignKey(product => product.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(product => product.AuctionWinner)
            .WithMany(user => user.WonAuctions)
            .HasForeignKey(product => product.AuctionWinnerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
