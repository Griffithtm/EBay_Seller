using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedback");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.SellerId).HasColumnName("sellerId");
        builder.Property(f => f.AverageRating).HasColumnName("averageRating").HasPrecision(3, 2);
        builder.Property(f => f.TotalReviews).HasColumnName("totalReviews");
        builder.Property(f => f.PositiveRate).HasColumnName("positiveRate").HasPrecision(5, 2);

        builder.HasOne(f => f.Seller)
            .WithOne(u => u.Feedback)
            .HasForeignKey<Feedback>(f => f.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.SellerId).IsUnique();
    }
}
