using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Review");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.ProductId).HasColumnName("productId");
        builder.Property(r => r.ReviewerId).HasColumnName("reviewerId");
        builder.Property(r => r.Rating).HasColumnName("rating");
        builder.Property(r => r.Comment).HasColumnName("comment").HasMaxLength(2000).IsRequired();
        builder.Ignore(r => r.Response);
        builder.Property(r => r.CreatedAt).HasColumnName("createdAt");

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => new { r.ProductId, r.ReviewerId }).IsUnique();
    }
}
