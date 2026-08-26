using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class SaleEventConfiguration : IEntityTypeConfiguration<SaleEvent>
{
    public void Configure(EntityTypeBuilder<SaleEvent> builder)
    {
        builder.ToTable("SaleEvent");
        builder.HasKey(saleEvent => saleEvent.Id);
        builder.Property(saleEvent => saleEvent.Id).HasColumnName("id");
        builder.Property(saleEvent => saleEvent.SellerId).HasColumnName("sellerId");
        builder.Property(saleEvent => saleEvent.Name).HasColumnName("name").HasMaxLength(90).IsRequired();
        builder.Property(saleEvent => saleEvent.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(saleEvent => saleEvent.Mode).HasColumnName("mode").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(saleEvent => saleEvent.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(saleEvent => saleEvent.StartDateUtc).HasColumnName("startDateUtc");
        builder.Property(saleEvent => saleEvent.EndDateUtc).HasColumnName("endDateUtc");
        builder.Property(saleEvent => saleEvent.OfferFreeShipping).HasColumnName("offerFreeShipping");
        builder.Property(saleEvent => saleEvent.IncludeSkippedItems).HasColumnName("includeSkippedItems");
        builder.Property(saleEvent => saleEvent.BlockPriceIncreaseRevisions).HasColumnName("blockPriceIncreaseRevisions");
        builder.Property(saleEvent => saleEvent.HighlightPercentage).HasColumnName("highlightPercentage").HasPrecision(5, 2);
        builder.Property(saleEvent => saleEvent.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(saleEvent => saleEvent.UpdatedAtUtc).HasColumnName("updatedAtUtc");

        builder.HasIndex(saleEvent => new { saleEvent.SellerId, saleEvent.Name }).IsUnique();
        builder.HasOne(saleEvent => saleEvent.Seller)
            .WithMany()
            .HasForeignKey(saleEvent => saleEvent.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
