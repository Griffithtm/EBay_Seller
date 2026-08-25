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
        builder.Property(r => r.RecipientId).HasColumnName("recipientId");
        builder.Property(r => r.ReviewerRole).HasColumnName("reviewerRole").HasMaxLength(20);
        builder.Property(r => r.RecipientRole).HasColumnName("recipientRole").HasMaxLength(20);
        builder.Property(r => r.Rating).HasColumnName("rating");
        builder.Property(r => r.RatingType).HasColumnName("ratingType").HasMaxLength(20);
        builder.Property(r => r.Comment).HasColumnName("comment").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Response).HasColumnName("response").HasMaxLength(2000);
        builder.Property(r => r.RepliedAt).HasColumnName("repliedAt");
        builder.Property(r => r.RevisionStatus).HasColumnName("revisionStatus").HasMaxLength(20);
        builder.Property(r => r.RevisionRequestedAt).HasColumnName("revisionRequestedAt");
        builder.Property(r => r.CreatedAt).HasColumnName("createdAt");

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Recipient)
            .WithMany()
            .HasForeignKey(r => r.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => new { r.ProductId, r.ReviewerId }).IsUnique();
    }
}
